/**************************************
                  Testing
 **************************************/


#include <Wire.h>
#include <Adafruit_ADS1015.h>
#include <EEPROM.h>

//TODO: Test the Texas Instrument ADS1256
Adafruit_ADS1115 loadCell;

int encoderPinA = 3;  //Pin associated with the encoder interruption
int encoderPinB = 4;
volatile int encoderDisplacement = 0;
int lastEncoderDisplacement = 0;
volatile unsigned long changingTime = 0;
unsigned long elapsedTime = 0;
unsigned long totalTime = 0;
unsigned long lastSampleTime = micros();
unsigned long sampleTime = 0;

//Version of the firmware
String version = "Race_Analyzer-0.2";

int pps = 10; //Pulses Per Sample. How many pulses are needed to get a sample
int ppsAddress = 0; //Where is stored the pps value in the EEPROM

int offset = 1030;
int offsetAddress = 2;

float calibrationFactor = 0.140142;
int calibrationAddress = 4;

float metersPerPulse = 0.003003;      //Value for the manual race encoder
int metersPerPulseAddress = 8;

//Wether the sensor has to capture or not
boolean capturing = true;

//Wether the encoder has reached the number of pulses per sample or not
boolean procesSample = false;

//wether the tranmission is in binary format or not
boolean binaryFormat = true;

//baud rate of the serial communication
unsigned long baudRate = 115200;

struct frame {
  unsigned long totalTime;
  //unsigned long elapsedTime;
  int offsettedData;
};

frame data;
int frameNumber = 0;

float sumFreq = 0;


long int totalForce = 0;
int nReadings = 0;
int offsettedData = 0;
int lastOffsettedData = 0;
float force = 0;
float meanFreq = 0;

void setup() {

  pinMode (encoderPinA, INPUT);
  pinMode (encoderPinB, INPUT);

  Serial.begin (baudRate);
  Serial.print("Initial setup: Baud rate : ");
  Serial.println(baudRate);

  Wire.setClock(1000000);

  EEPROM.get(ppsAddress, pps);
  //if pps is 0 it means that it has never been set. We use the default value
  if (pps == -151) {
    pps = 10;
    EEPROM.put(ppsAddress, pps);
  }

  loadCell.begin();
  loadCell.setGain(GAIN_ONE);

  EEPROM.get(offsetAddress, offset);
  //if offset is -1 it means that it has never been set. We use the default value
  if (offset == -1) {
    offset = 1030;
    EEPROM.put(offsetAddress, offset);
  }

  EEPROM.get(calibrationAddress, calibrationFactor);
  //if calibrationFactor is not a number it means that it has never been set. We use the default value
  if (isnan(calibrationFactor)) {
    calibrationFactor = 0.140142;
    EEPROM.put(calibrationAddress, calibrationFactor);
  }

  EEPROM.get(metersPerPulseAddress, metersPerPulse);

  //if metersPerPulse is not a number it means that it has never been set. We use the default value
  if (isnan(metersPerPulse)) {
    metersPerPulse = 0.003003;
    EEPROM.put(metersPerPulseAddress, metersPerPulse);
  }

  //Using the rising flank of the A photocell we have a normal PPR.
  attachInterrupt(digitalPinToInterrupt(encoderPinA), changingA, RISING);

  //Using the CHANGE with both photocells WE CAN HAVE four times normal PPR
  //attachInterrupt(digitalPinToInterrupt(encoderPinB), changingB, CHANGE);

  //  start_capture();

  //lastOffsettedData = readOffsettedData(0);
}

void loop() {

  if (capturing)
  {

    //TODO: If the value of the force is negative, send a 0
    while (!procesSample) {

      //      !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
      //      ¡¡¡¡¡¡¡¡¡ Atention. Reading the ADC is really slow and uses a lot of interruptions to transfer the data!!!!!!!!
      //      !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

      //If some string comes from Serial process the sample
      //This makes that the sample after getting the command "end_capture:" has less pulses than pps
      if (Serial.available() > 0) {
        sampleTime = micros();
        lastEncoderDisplacement = encoderDisplacement;
        encoderDisplacement = 0;
        procesSample = true;
      }
    }

    //Managing the timer overflow
    if (sampleTime > lastSampleTime)      //No overflow
    {
      elapsedTime = sampleTime -  lastSampleTime;
    } else  if (sampleTime <=  lastSampleTime)  //Overflow
    {
      elapsedTime = (4294967295 -  lastSampleTime) + sampleTime; //Time from the last measure to the overflow event plus the changingTime
    }
    totalTime += elapsedTime;
    //double meanForce = totalForce / nReadings;
    lastSampleTime = sampleTime;

    //Print the frequency
    //Serial.println( float(pps) / (float(elapsedTime) / 1000000.0) );

    //The frame is stored in a structure in order to make only one Serial.write
    data.totalTime = totalTime;
    //data[frameNumber].elapsedTime = elapsedTime;
    //offsettedData = readOffsettedData(0);

    //The force is not measured in the instant of the sample but the mean force fot he current and last sample
    data.offsettedData = 0;
    //lastOffsettedData = offsettedData;
    //sumFreq = sumFreq + 1000000.0 * pps / float(elapsedTime);

    //Printing in binary format
    Serial.write((byte*)&data, 6);

    //Printing in text mode
    //        Serial.print(totalTime);
    //        Serial.print("\t");
    //        Serial.println(elapsedTime);
    //        Serial.println(data[frameNumber].offsettedData);


    procesSample = false;       //Keep acumulating pulses without processing the sample until [pps] samples is reached
  }
}

void changingA() {

  if (digitalRead(encoderPinB) == HIGH) {
    encoderDisplacement--;
  } else {
    encoderDisplacement++;
  }

  //only measures time if the number of pulses is [pps]
  if (abs(encoderDisplacement) >= pps) {
    sampleTime = micros();
    procesSample = true;
    encoderDisplacement = 0;
  }

}

void serialEvent()
{
  //Sending a command interrupts the data aquisition process
  detachInterrupt(digitalPinToInterrupt(encoderPinA));
  capturing = false;
  procesSample = false;
  String inputString = Serial.readString();
  String commandString = inputString.substring(0, inputString.lastIndexOf(":"));
  if (commandString == "start_capture") {
    start_capture();
  } else if (commandString == "end_capture") {
    end_capture();
  } else if (commandString == "get_version") {
    get_version();
  } else if (commandString == "set_pps") {
    set_pps(inputString);
  } else if (commandString == "get_pps") {
    get_pps();
  } else if (commandString == "tare") {
    tare();
  } else if (commandString == "get_transmission_format") {
    get_transmission_format();
  } else if (commandString == "get_calibration_factor") {
    get_calibration_factor();
  } else if (commandString == "set_calibration_factor") {
    set_calibration_factor(inputString);
  } else if (commandString == "calibrate") {
    calibrate(inputString);
  } else if (commandString == "get_offset") {
    get_offset();
  } else if (commandString == "set_offset") {
    set_offset(inputString);
  } else if (commandString == "get_mpp") {
    get_mpp();
  } else if (commandString == "get_baud_rate") {
    get_baud_rate();
  } else if (commandString == "set_baud_rate") {
    set_baud_rate(inputString);
  } else if (commandString == "start_simulation") {
    start_simulation();
  } else {
    Serial.println("Not a valid command");
  }
  inputString = "";
}

void start_capture()
{
  Serial.flush();
  Serial.print("Starting capture;PPS:");
  Serial.println(pps);


  attachInterrupt(digitalPinToInterrupt(encoderPinA), changingA, RISING);

  totalTime = 0;
  lastSampleTime = micros();
  sampleTime = lastSampleTime + 1;

  //First sample with a low speed is mandatory to good detection of the start

  //sendInt(0);
  sendLong(1);
  sendInt(0);     //Fake force. Fixed number for debugging

  capturing = true;
  encoderDisplacement = 0;
}

void end_capture()
{
  capturing = false;

  //sendLong(4294967295);
  //sendInt(32767);
  Serial.print("\nCapture ended:");
  Serial.print("TotalTime :");
  Serial.println(totalTime);
}

void get_version()
{
  Serial.println(version);
}

//Setting how many pulses are needed to get a sample
void set_pps(String inputString)
{
  String argument = get_command_argument(inputString);
  int newPps = argument.toInt();
  if (newPps != pps) {  //Trying to reduce the number of writings
    EEPROM.put(ppsAddress, newPps);
    pps = newPps;
  }
  Serial.print("pps set to: ");
  Serial.println(pps);
}

void get_pps()
{
  Serial.println(pps);
}

String get_command_argument(String inputString)
{
  return (inputString.substring(inputString.lastIndexOf(":") + 1, inputString.lastIndexOf(";")));
}

void tare(void)
{
  long int totalForce = 0;
  for (int i = 1; i <= 100;  i++)
  {
    totalForce += loadCell.readADC_SingleEnded(0);
  }

  offset = totalForce / 100;
  EEPROM.put(offsetAddress, offset);
  Serial.println("Taring OK");
}

int readOffsettedData(int sensor)
{
  return (loadCell.readADC_SingleEnded(sensor) - offset);
}

void calibrate(String inputString)
{
  //Reading the argument of the command. Located within the ":" and the ";"
  String loadString = get_command_argument(inputString);
  float load = loadString.toFloat();
  float totalForce = 0;
  for (int i = 1; i <= 1000;  i++)
  {
    totalForce += readOffsettedData(0);
  }

  calibrationFactor = load * 9.81 / (totalForce / 1000.0);
  EEPROM.put(calibrationAddress, calibrationFactor);
  Serial.println("Calibrating OK");
}

float readForce(int sensor)
{
  int offsettedData = readOffsettedData(0);
  float force = calibrationFactor * (float)offsettedData;
  return (force);
}

void sendInt(int i) {
  byte * b = (byte *) &i;
  Serial.write(b, 2);
  return;
}

void sendLong(long l) {
  byte * b = (byte *) &l;
  Serial.write(b, 4);
  return;
}


void sendFloat(float f) {
  byte * b = (byte *) &f;
  Serial.write(b[0]); //Least significant byte
  Serial.write(b[1]);
  Serial.write(b[2]);
  Serial.write(b[3]); //Most significant byte
  Serial.flush();
  return;
}

void get_transmission_format()
{
  if (binaryFormat)
  {
    Serial.println("binary");
  } else
  {
    Serial.println("text");
  }
}

void set_calibration_factor(String inputString)
{
  //Reading the argument of the command. Located within the ":" and the ";"
  String calibration_factor = get_command_argument(inputString);
  //Serial.println(calibration_factor.toFloat());
  calibrationFactor = calibration_factor.toFloat();
  float stored_calibration = 0.0f;
  EEPROM.get(calibrationAddress, stored_calibration);
  if (stored_calibration != calibrationFactor) {
    EEPROM.put(calibrationAddress, calibrationFactor);
  }
  Serial.println("Calibration factor set");
}

void set_offset(String inputString)
{
  String offsetString = get_command_argument(inputString);
  long value = offsetString.toInt();
  offset = value;
  int stored_offset = 0;
  EEPROM.get(offsetAddress, stored_offset);
  if (stored_offset != value) {
    EEPROM.put(offsetAddress, value);
    Serial.println("updated");
  }
  Serial.println("offset set");
}

void get_offset(void)
{
  int stored_offset = 0;
  EEPROM.get(offsetAddress, stored_offset);
  Serial.println(stored_offset);
}

void get_calibration_factor(void)
{
  Serial.println(calibrationFactor, 8);
}

void get_mpp(void)
{
  Serial.println(metersPerPulse, 8);
}

void get_baud_rate(void)
{
  Serial.println(baudRate);
}

void set_baud_rate(String inputString)
{
  String baudRateString = get_command_argument(inputString);
  baudRate = baudRateString.toInt();
  //Serial.end();
  Serial.print("InpuptString = ");
  Serial.println(baudRateString);
  Serial.print("Going to change the baud rate ");
  Serial.println(baudRate);
  Serial.flush();
  Serial.begin(baudRate);
  Serial.flush();
  delay(10000);
  Serial.print("Setting baud rate to ");
  Serial.println(baudRate);
}

//TODO: Write in binary mode
void start_simulation(void)
{
  float vmax = 10.0;
  float k = 1.0;
  float currentPosition = 0.0;
  float lastPosition = 0.0;
  float displacement;


  Serial.println("Starting capture...");

  Serial.print(0);
  Serial.print(";");
  Serial.print(1);
  Serial.print(";");
  Serial.println(0);

  Serial.print(0);
  Serial.print(";");
  Serial.print(round(1E4));
  Serial.print(";");
  Serial.println(0);

  for (float totalTime = 2E4; totalTime < 1E7; totalTime = totalTime + 1E4)
  {
    //Sending in text mode
    //    Serial.print(round( displacement / 0.003003 ));
    Serial.print(0);
    Serial.print(";");
    Serial.print(round(totalTime));
    Serial.print(";");
    Serial.println(0);

  }
  for (float totalTime = 0; totalTime <= 1E7; totalTime = totalTime + 1E4)
  {
    currentPosition = vmax * (totalTime / 1E6 + pow(2.7182818, (-k * totalTime / 1E6)) / k ) - vmax / k ;
    displacement = currentPosition - lastPosition;

    //Sending in text mode
    //    Serial.print(round( displacement / 0.003003 ));
    Serial.print(round( displacement ));
    Serial.print(";");
    Serial.print(round(totalTime + 1E7));
    Serial.print(";");
    Serial.println(0);
  }
}
