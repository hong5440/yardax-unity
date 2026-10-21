/**
 * 미니 골리앗 크레인 펌웨어 (ESP32)
 *
 * - 스테퍼 3개: 주행 좌/우(동기) + 횡행, 서보 1개: 권상
 * - 시리얼(115200)로 Yard Hub 메시지 수신: operator_input / interlock / heartbeat / fault
 * - 안전: 리밋 스위치 4개(방향별 차단), 비상정지 NC(열리면 즉시 정지),
 *         heartbeat 500ms 초과 시 정지 (fail-safe)
 * - 50ms마다 crane_state 보고 (스텝 수 → cm 환산)
 *
 * 메시지 형식은 저장소 docs/protocol.md 를 따른다. 예:
 *   수신: {"type":"operator_input","axis":"GANTRY","dir":"+","amount":20}
 *   송신: {"type":"crane_state","crane":"goliath-mini","travel_cm":12.5,...}
 */

#include <AccelStepper.h>
#include <ArduinoJson.h>
#include <ESP32Servo.h>

#include "config.h"

AccelStepper gantryL(AccelStepper::DRIVER, PIN_GANTRY_L_STEP, PIN_GANTRY_L_DIR);
AccelStepper gantryR(AccelStepper::DRIVER, PIN_GANTRY_R_STEP, PIN_GANTRY_R_DIR);
AccelStepper trolley(AccelStepper::DRIVER, PIN_TROLLEY_STEP, PIN_TROLLEY_DIR);
Servo hoist;

static float hoistTargetDeg = HOIST_HOME_DEG;
static float hoistCurrentDeg = HOIST_HOME_DEG;
static bool interlocked = false;
static bool estopped = false;
static unsigned long lastHeartbeat = 0;
static unsigned long lastReport = 0;
static char lineBuf[256];
static size_t lineLen = 0;

// ── 정지 ────────────────────────────────────────────────
static void stopAll(const char *reason) {
  gantryL.stop();  // 감속 정지 목표 설정
  gantryR.stop();
  trolley.stop();
  // 즉시 정지가 필요한 사유(비상정지·하트비트)면 목표를 현재 위치로
  gantryL.moveTo(gantryL.currentPosition());
  gantryR.moveTo(gantryR.currentPosition());
  trolley.moveTo(trolley.currentPosition());
  hoistTargetDeg = hoistCurrentDeg;

  JsonDocument doc;
  doc["type"] = "fault";
  doc["crane"] = CRANE_ID;
  doc["reason"] = reason;
  doc["ts"] = millis();
  serializeJson(doc, Serial);
  Serial.println();
}

// ── 안전 입력 ───────────────────────────────────────────
static bool limitHit(int pin) { return digitalRead(pin) == LOW; }

static void checkSafety() {
  // 비상정지 NC: 체인이 열리면(HIGH) 즉시 정지
  bool open = digitalRead(PIN_ESTOP) == HIGH;
  if (open && !estopped) {
    estopped = true;
    stopAll("estop_open");
  } else if (!open && estopped) {
    estopped = false;  // 해제는 보고만 — 재동작은 새 명령부터
  }

  // 하트비트 타임아웃
  if (lastHeartbeat != 0 && millis() - lastHeartbeat > HEARTBEAT_TIMEOUT_MS) {
    lastHeartbeat = 0;  // 1회만 보고
    stopAll("heartbeat_timeout");
  }

  // 리밋 스위치: 해당 방향 진행 중이면 그 축 정지
  if (limitHit(PIN_LIMIT_GANTRY_MIN) && gantryL.targetPosition() < gantryL.currentPosition())
    stopAll("limit_gantry_min");
  if (limitHit(PIN_LIMIT_GANTRY_MAX) && gantryL.targetPosition() > gantryL.currentPosition())
    stopAll("limit_gantry_max");
  if (limitHit(PIN_LIMIT_TROLLEY_MIN) && trolley.targetPosition() < trolley.currentPosition())
    stopAll("limit_trolley_min");
  if (limitHit(PIN_LIMIT_TROLLEY_MAX) && trolley.targetPosition() > trolley.currentPosition())
    stopAll("limit_trolley_max");
}

static bool motionAllowed() {
  return !estopped && !interlocked && lastHeartbeat != 0;
}

// ── 명령 처리 ───────────────────────────────────────────
static void handleOperatorInput(JsonDocument &doc) {
  if (!motionAllowed()) {
    stopAll(estopped ? "blocked_estop" : (interlocked ? "blocked_interlock" : "blocked_no_heartbeat"));
    return;
  }
  const char *axis = doc["axis"] | "";
  const char *dirS = doc["dir"] | "+";
  float amount = doc["amount"] | 0.0f;  // cm (권상도 cm → 각도 환산)
  int sgn = (dirS[0] == '-') ? -1 : +1;

  if (strcmp(axis, "GANTRY") == 0) {
    long delta = lroundf(sgn * amount * STEPS_PER_CM_GANTRY);
    long limit = lroundf(GANTRY_RANGE_CM * STEPS_PER_CM_GANTRY);
    long target = constrain(gantryL.currentPosition() + delta, 0L, limit);
    gantryL.moveTo(target);  // 좌/우 동기: 같은 목표
    gantryR.moveTo(target);
  } else if (strcmp(axis, "TROLLEY") == 0) {
    long delta = lroundf(sgn * amount * STEPS_PER_CM_TROLLEY);
    long limit = lroundf(TROLLEY_RANGE_CM * STEPS_PER_CM_TROLLEY);
    trolley.moveTo(constrain(trolley.currentPosition() + delta, 0L, limit));
  } else if (strcmp(axis, "HOIST") == 0) {
    hoistTargetDeg = constrain(hoistTargetDeg + sgn * amount * HOIST_DEG_PER_CM,
                               (float)HOIST_MIN_DEG, (float)HOIST_MAX_DEG);
  }
}

static void handleLine(const char *line) {
  JsonDocument doc;
  if (deserializeJson(doc, line) != DeserializationError::Ok) return;
  const char *type = doc["type"] | "";

  if (strcmp(type, "heartbeat") == 0) {
    lastHeartbeat = millis();
  } else if (strcmp(type, "operator_input") == 0) {
    handleOperatorInput(doc);
  } else if (strcmp(type, "interlock") == 0) {
    interlocked = doc["active"] | false;
    if (interlocked) stopAll("interlock");
  } else if (strcmp(type, "fault") == 0) {
    stopAll("hub_fault");
  }
}

// ── 상태 보고 ───────────────────────────────────────────
static void reportState() {
  JsonDocument doc;
  doc["type"] = "crane_state";
  doc["crane"] = CRANE_ID;
  doc["travel_cm"] = gantryL.currentPosition() / STEPS_PER_CM_GANTRY;
  doc["trolley_cm"] = trolley.currentPosition() / STEPS_PER_CM_TROLLEY;
  doc["hook_cm"] = (hoistCurrentDeg - HOIST_MIN_DEG) / HOIST_DEG_PER_CM;
  doc["sync_err_steps"] = (long)(gantryL.currentPosition() - gantryR.currentPosition());
  doc["estop"] = estopped;
  doc["interlock"] = interlocked;
  doc["ts"] = millis();
  serializeJson(doc, Serial);
  Serial.println();
}

// ── 메인 ────────────────────────────────────────────────
void setup() {
  Serial.begin(SERIAL_BAUD);

  pinMode(PIN_LIMIT_GANTRY_MIN, INPUT_PULLUP);
  pinMode(PIN_LIMIT_GANTRY_MAX, INPUT_PULLUP);
  pinMode(PIN_LIMIT_TROLLEY_MIN, INPUT_PULLUP);
  pinMode(PIN_LIMIT_TROLLEY_MAX, INPUT_PULLUP);
  pinMode(PIN_ESTOP, INPUT_PULLUP);

  gantryL.setMaxSpeed(GANTRY_MAX_SPEED);
  gantryL.setAcceleration(GANTRY_ACCEL);
  gantryR.setMaxSpeed(GANTRY_MAX_SPEED);
  gantryR.setAcceleration(GANTRY_ACCEL);
  trolley.setMaxSpeed(TROLLEY_MAX_SPEED);
  trolley.setAcceleration(TROLLEY_ACCEL);

  hoist.setPeriodHertz(50);
  hoist.attach(PIN_HOIST_SERVO, 500, 2400);
  hoist.write((int)hoistCurrentDeg);
}

void loop() {
  // 시리얼 라인 수신
  while (Serial.available()) {
    char c = (char)Serial.read();
    if (c == '\n' || c == '\r') {
      if (lineLen > 0) {
        lineBuf[lineLen] = '\0';
        handleLine(lineBuf);
        lineLen = 0;
      }
    } else if (lineLen < sizeof(lineBuf) - 1) {
      lineBuf[lineLen++] = c;
    } else {
      lineLen = 0;  // 과길이 라인 폐기
    }
  }

  checkSafety();

  if (motionAllowed()) {
    gantryL.run();
    gantryR.run();
    trolley.run();
    // 권상 서보: 완만히 목표 각도로
    float d = hoistTargetDeg - hoistCurrentDeg;
    if (fabsf(d) > 0.5f) {
      hoistCurrentDeg += constrain(d, -2.0f, 2.0f);
      hoist.write((int)hoistCurrentDeg);
    }
  }

  if (millis() - lastReport >= STATE_REPORT_MS) {
    lastReport = millis();
    reportState();
  }
}
