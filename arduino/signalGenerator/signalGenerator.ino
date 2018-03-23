/*
signalGenerator: Generates a digital signal following different sequences defined in sequences[]

Copyright (C) 2018 Xavier de Blas xaviblas@gmail.com
Copyright (C) 2018 Xavier Padullés support@chronojump.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#define   signalPin   2
#define   ledPin      13

//first num is the last element position
//2nd param is start mode
//values are in milliseconds
const String sequences [] = {
  "5;IN;100;1500;200;5000",
  "6;OUT;1100;40;1200;30;8000"
};

void setup() {
  pinMode(signalPin, OUTPUT);
  Serial.begin(9600);
}

void loop() {
  //signalOn(500);
  //signalOff(100);

  //always read 2nd string. TODO: do it by serial port
  int strNum = 0;
  processString(strNum);
}

void processString(int n)
{
  String sequence = sequences[n]; //TODO: check n is not greater than sequences length

  int last = getValue(sequence, ';', 0).toInt();
  String currentStatus = getValue(sequence, ';', 1);
  for (int i = 2; i <= last; i++)
  {
    int duration = getValue(sequence, ';', i).toInt();

    if (currentStatus == "IN")
      signalOn(duration);
    else
      signalOff(duration);

    //invert status
    if(currentStatus == "IN")
      currentStatus = "OUT";
    else
      currentStatus = "IN";
  }
}

// https://stackoverflow.com/questions/9072320/split-string-into-string-array
String getValue(String data, char separator, int index)
{
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length() - 1;

  for (int i = 0; i <= maxIndex && found <= index; i++) {
    if (data.charAt(i) == separator || i == maxIndex) {
      found++;
      strIndex[0] = strIndex[1] + 1;
      strIndex[1] = (i == maxIndex) ? i + 1 : i;
    }
  }

  return found > index ? data.substring(strIndex[0], strIndex[1]) : "";
}

void signalOn(int duration) {
  Serial.print("\nsignalON ");
  Serial.println(duration);
  digitalWrite(signalPin, HIGH);
  digitalWrite(ledPin, HIGH);
  delay(duration);
}

void signalOff(int duration) {
  Serial.print("\nsignalOFF ");
  Serial.println(duration);
  digitalWrite(signalPin, LOW);
  digitalWrite(ledPin, LOW);
  delay(duration);
}

