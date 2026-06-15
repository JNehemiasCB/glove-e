/*
 * GLOVE-E : Guante ultrasónico con BLE
 * ------------------------------------
 * Mantiene la lógica original (HC-SR04 + motor vibrador + buzzer)
 * y agrega un servidor BLE (GATT) que:
 *   - NOTIFICA la distancia medida cada NOTIFY_INTERVAL ms
 *   - RECIBE configuración desde la app ("MAX:100;CRIT:30")
 *
 * Placa: ESP32 (Arduino core 2.x o superior)
 */

#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

// ---------- Pines (igual que tu diagrama) ----------
const int TRIG_PIN   = 5;
const int ECHO_PIN   = 18;
const int MOTOR_PIN  = 19;
const int BUZZER_PIN = 21;

// ---------- Rangos (ahora variables: la app puede cambiarlos) ----------
int rangoMaximo  = 100;   // cm: empieza a vibrar
int rangoCritico = 30;    // cm: suena el buzzer

// ---------- UUIDs BLE (deben coincidir con la app) ----------
#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHAR_DISTANCE_UUID  "beb5483e-36e1-4688-b7f5-ea07361b26a8"  // Notify: distancia
#define CHAR_CONFIG_UUID    "5a87b4ef-3bfa-4eb2-a9c8-71d18d6b1e22"  // Write: configuración

#define DEVICE_NAME "GloveE"
const unsigned long NOTIFY_INTERVAL = 300; // ms entre notificaciones BLE

BLEServer*         pServer = nullptr;
BLECharacteristic* pDistanceChar = nullptr;
bool deviceConnected = false;
unsigned long lastNotify = 0;

// ---------- Callbacks de conexión ----------
class ServerCallbacks : public BLEServerCallbacks {
  void onConnect(BLEServer* s) override {
    deviceConnected = true;
    Serial.println("App conectada por BLE");
  }
  void onDisconnect(BLEServer* s) override {
    deviceConnected = false;
    Serial.println("App desconectada. Re-publicando...");
    s->getAdvertising()->start(); // volver a anunciarse
  }
};

// ---------- Callback de configuración (la app escribe aquí) ----------
// Formato esperado: "MAX:100;CRIT:30"
class ConfigCallbacks : public BLECharacteristicCallbacks {
  void onWrite(BLECharacteristic* c) override {
    String valor = String(c->getValue().c_str());
    Serial.print("Config recibida: ");
    Serial.println(valor);

    int iMax  = valor.indexOf("MAX:");
    int iCrit = valor.indexOf("CRIT:");
    if (iMax >= 0) {
      int v = valor.substring(iMax + 4).toInt();
      if (v >= 50 && v <= 300) rangoMaximo = v;
    }
    if (iCrit >= 0) {
      int v = valor.substring(iCrit + 5).toInt();
      if (v >= 10 && v < rangoMaximo) rangoCritico = v;
    }
    Serial.printf("Rangos -> MAX:%d CRIT:%d\n", rangoMaximo, rangoCritico);
  }
};

void setup() {
  Serial.begin(115200);

  pinMode(TRIG_PIN, OUTPUT);
  pinMode(ECHO_PIN, INPUT);
  pinMode(BUZZER_PIN, OUTPUT);
  pinMode(MOTOR_PIN, OUTPUT);

  digitalWrite(BUZZER_PIN, LOW);
  analogWrite(MOTOR_PIN, 0);

  // ---------- Inicializar BLE ----------
  BLEDevice::init(DEVICE_NAME);
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new ServerCallbacks());

  BLEService* pService = pServer->createService(SERVICE_UUID);

  // Característica de distancia (Read + Notify)
  pDistanceChar = pService->createCharacteristic(
      CHAR_DISTANCE_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY);
  pDistanceChar->addDescriptor(new BLE2902()); // necesario para Notify

  // Característica de configuración (Write)
  BLECharacteristic* pConfigChar = pService->createCharacteristic(
      CHAR_CONFIG_UUID,
      BLECharacteristic::PROPERTY_WRITE);
  pConfigChar->setCallbacks(new ConfigCallbacks());

  pService->start();
  pServer->getAdvertising()->addServiceUUID(SERVICE_UUID);
  pServer->getAdvertising()->start();
  Serial.println("BLE listo. Anunciando como 'GloveE'");
}

int medirDistancia() {
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);

  long duracion = pulseIn(ECHO_PIN, HIGH, 30000); // timeout 30ms evita bloqueos
  return duracion * 0.034 / 2;
}

void loop() {
  int distancia = medirDistancia();

  Serial.print("Distancia: ");
  Serial.print(distancia);
  Serial.println(" cm");

  // ---------- Lógica de actuadores (igual que tu versión) ----------
  if (distancia <= 0 || distancia > 300) {
    analogWrite(MOTOR_PIN, 0);
    digitalWrite(BUZZER_PIN, LOW);
  }
  else if (distancia <= rangoMaximo && distancia > rangoCritico) {
    int intensidad = map(distancia, rangoMaximo, rangoCritico, 100, 255);
    analogWrite(MOTOR_PIN, intensidad);
    digitalWrite(BUZZER_PIN, LOW);
  }
  else if (distancia <= rangoCritico) {
    analogWrite(MOTOR_PIN, 255);
    digitalWrite(BUZZER_PIN, HIGH);
  }
  else {
    analogWrite(MOTOR_PIN, 0);
    digitalWrite(BUZZER_PIN, LOW);
  }

  // ---------- Notificar a la app por BLE ----------
  if (deviceConnected && millis() - lastNotify >= NOTIFY_INTERVAL) {
    lastNotify = millis();
    if (distancia > 0 && distancia <= 300) {
      char buf[8];
      snprintf(buf, sizeof(buf), "%d", distancia);
      pDistanceChar->setValue(buf);
      pDistanceChar->notify();
    }
  }

  delay(100);
}
