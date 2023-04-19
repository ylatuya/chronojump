

//Manages the current menu
void showMenu()
{
  //The blue button navigates through the Menu options
  
  rightButton.update();
  leftButton.update();
  if (leftButton.fell()) {
    currentMenuIndex--;
    currentMenuIndex = currentMenuIndex % menuItemsNum;
    if (currentMenuIndex < 0) {
      currentMenuIndex = menuItemsNum - 1;
    }
    leftButtonPressed = true;
    showMenuEntry(currentMenuIndex);
  }
  
  if (rightButton.fell()) {
    currentMenuIndex++;
    currentMenuIndex = currentMenuIndex % menuItemsNum;
    rightButtonPressed = true; 
    showMenuEntry(currentMenuIndex);
  }

  //The red button activates the menu option
  cenButton.update();
  if (cenButton.fell())
  {
    PcControlled = false;
    currentMenu[currentMenuIndex].function();
  }
}

//Exits the currentMenu and sets the currentMenu to mainMenu
void backMenu(void)
{
  currentMenuIndex = 0;
  drawMenuBackground();
  currentMenuIndex = 0;
  for (int i = 0; i < 10; i++) {
    currentMenu[i].title = mainMenu[i].title;
    currentMenu[i].description = mainMenu[i].description;
    currentMenu[i].function = mainMenu[i].function;
  }
  menuItemsNum = mainMenuItems;
  showMenuEntry(currentMenuIndex);
  showMenu();
}

//Erases screen and draw the left and right buttons in the upper part of screen
void drawMenuBackground() {
  tft.fillScreen(BLACK);
  tft.writeRect(0, 20, 25, 25, (uint16_t*)left);
  tft.writeRect(295, 20, 25, 25, (uint16_t*)right);
  tft.writeRect(145, 215, 25, 25, (uint16_t*)center);
  printTftText("Enter",143, 210, WHITE, 1);
}

//Set the currentMenu to systemMenu and shows it
void showSystemMenu(void)
{
  /*
  if (!selectExerciseType(jumps) || !selectExerciseType(inertial) || !selectExerciseType(force) || !selectExerciseType(encoderRace)) 
  {
    //Try adding showmenu & backmenu functions    
    return;
  }
  */  
  drawMenuBackground();
  currentMenuIndex = 0;
  for (int i = 0; i < 10; i++) {
    currentMenu[i].title = systemMenu[i].title;
    currentMenu[i].description = systemMenu[i].description;
    currentMenu[i].function = systemMenu[i].function;
  }
  //menuItemsNum = systemMenuItems;
  //Create a new function to navigate through the system menu
  //showMenuEntry(currentMenuIndex);
  showSystemEntry(currentMenuIndex);
  //showMenu();
}

void showSystemEntry(unsigned int currentMenuIndex)
{
  tft.fillRect(30, 0, 260, 50, BLACK);
  printTftText(currentMenu[currentMenuIndex].title, 40, 20, WHITE, 3);
  //This erases the last index description
  rightButton.update();
  leftButton.update();
  Serial.println(systemMenuItems);  
  if (rightButtonPressed) {
    printTftText(currentMenu[(currentMenuIndex + systemMenuItems - 1) % systemMenuItems].description, 12, 100, BLACK);   
  }
  else if (leftButtonPressed) { //Fordwards
    printTftText(currentMenu[(currentMenuIndex + systemMenuItems + 1) % systemMenuItems].description, 12, 100, BLACK);
  }
    
  rightButton.update();
  leftButton.update();
  Serial.println(currentMenuIndex);
  printTftText(currentMenu[currentMenuIndex].description, 12, 100); 
}

//shows the current entry of the current menu
void showMenuEntry(unsigned int currentMenuIndex)
{
  tft.fillRect(30, 0, 260, 50, BLACK);
  printTftText(currentMenu[currentMenuIndex].title, 40, 20, WHITE, 3);
  //This erases the last index description
  rightButton.update();
  leftButton.update();  
  if (rightButtonPressed) {
    printTftText(currentMenu[(currentMenuIndex + menuItemsNum - 1) % menuItemsNum].description, 12, 100, BLACK);
    rightButtonPressed = false;
  }
  else if (leftButtonPressed) {
    printTftText(currentMenu[(currentMenuIndex + menuItemsNum + 1) % menuItemsNum].description, 12, 100, BLACK);     
    leftButtonPressed = false;
  }
  else if (leftButtonPressed && leftButton.fell()) {
    printTftText(currentMenu[(currentMenuIndex + menuItemsNum + 1) % menuItemsNum].description, 12, 100, BLACK);     
    leftButtonPressed = false;
  }
  rightButton.update();
  leftButton.update();
  printTftText(currentMenu[currentMenuIndex].description, 12, 100);
}

void drawRightButton(void) { drawRightButton(295, 212, "", WHITE, BLACK); }
void drawRightButton(String label) {drawRightButton(295, 206, label, WHITE, BLACK); }
void drawRightButton(String label, uint16_t tColor) {drawRightButton(295, 206, label, tColor, BLACK); }
void drawRightButton(String label, uint16_t tColor, uint16_t bColor) {drawRightButton(295, 206, label, tColor, bColor); }
void drawRightButton(int x, int y) { drawRightButton(x, y, "", WHITE, BLACK); }
void drawRightButton(int x, int y, String label) { drawRightButton(x, y, label, WHITE, BLACK); }
void drawRightButton(int x, int y, String label, uint16_t tColor) { drawRightButton(x, y, label, tColor, BLACK); }
void drawRightButton(int x, int y, String label, uint16_t tColor, uint16_t bColor)
{
  tft.writeRect(x, y, 25, 25, (uint16_t*)right);
  if (label.length() > 0) {
    printTftText(label, x-1, y+6, tColor, 2, true);
    }
}

void drawLeftButton(void) { drawLeftButton(0, 206, "", WHITE, BLACK); }
void drawLeftButton(String label) {drawLeftButton(0, 206, label, WHITE, BLACK); }
void drawLeftButton(String label, uint16_t tColor) {drawLeftButton(0, 206, label, tColor, BLACK); }
void drawLeftButton(String label, uint16_t tColor, uint16_t bColor) {drawLeftButton(0, 206, label, tColor, bColor); }
void drawLeftButton(int x, int y) { drawLeftButton(x, y, "", WHITE, BLACK); }
void drawLeftButton(int x, int y, String label) { drawLeftButton(x, y, label, WHITE, BLACK); }
void drawLeftButton(int x, int y, String label, uint16_t tColor) { drawLeftButton(x, y, label, tColor, BLACK); }
void drawLeftButton(int x, int y, String label, uint16_t tColor, uint16_t bColor)
{
  tft.writeRect(x, y, 25, 25, (uint16_t*)left);
  if (label.length() > 0) {
    printTftText(label, x + 26, y+6, tColor, 2);
  }
}

//Dialog for selecting float value
float selectValueDialog(String description, String rangesString, String incString) {
  return selectValueDialog(description, rangesString, incString,0);
}
float selectValueDialog(String description, String rangesString, String incString, unsigned int decimals)
{
  //ranges are of the format "1,10,500"
  //increments are in the format of  "2,10"
  //From 1..10 increment by 2
  //From 10..500 increment by 10
  //increments must have the number of ranges elements -1
  //Maximum number of ranges is 10 (11 key values)
  int prevColon = 0;
  int nextColon = rangesString.indexOf(",");
  unsigned int rangesNum = 0;

  //Counting ranges
  do
  {
    rangesNum++;
    prevColon = nextColon + 1;
    nextColon = rangesString.indexOf(",", prevColon);
  } while (nextColon != -1);
  float rangesValues[11];
  float incValues[10];

  //Assigning key values of the ranges
  prevColon = 0;
  nextColon = rangesString.indexOf(",");
  for (unsigned int i = 0; i <= rangesNum; i++)
  {
    rangesValues[i] = rangesString.substring(prevColon, nextColon).toFloat();
    prevColon = nextColon + 1;
    nextColon = rangesString.indexOf(",", prevColon);
  }

  //Assigning increment values
  prevColon = 0;
  nextColon = incString.indexOf(",");
  for (unsigned int i = 0; i < rangesNum; i++)
  {
    incValues[i] = incString.substring(prevColon, nextColon).toFloat();
    prevColon = nextColon + 1;
    nextColon = incString.indexOf(",", prevColon);
  }

  float value = rangesValues[0];
  submenu = 0;
  int currentSegment = 1;
  bool exitFlag = false;
  //Delete description
  tft.fillRect(0, 50, 320, 190, BLACK);

//  tft.setCursor(30, 80);
//  tft.print(title);

  //Explanation of the process
  printTftText(description, 10, 112);

  //Blue button
  //drawLeftButton("+" + String(incValues[0], decimals), WHITE, BLUE);

  //Red button
  //drawRightButton("Accept", WHITE, RED);

  drawLeftButton("-" + String(incValues[currentSegment - 1], decimals), WHITE, RED);
  drawRightButton("+" + String(incValues[currentSegment - 1], decimals), WHITE, BLUE);

  //Current value
  printTftText("Current:", 100, 174);
  printTftValue(value, 236, 174, 2, 0);
  cenButton.update();
  rightButton.update();
  leftButton.update();
  
  while (!exitFlag) {

    //Selecting the force goal
    //TODO: Allow coninuous increasing by keeping pressed the button
    if (rightButton.fell()) {
      printTftValue(value, 236, 174, 2, decimals, BLACK);
      
      value += incValues[currentSegment - 1];
      if (abs(value -  rangesValues[rangesNum] - incValues[currentSegment - 1]) < 0.0001) {
        printTftValue(value, 236, 174, 2, decimals, BLACK);
        value = rangesValues[0];
        currentSegment = 1;
        drawLeftButton("+" + String(incValues[currentSegment - 1], decimals), WHITE, RED);
      }
      //Sometimes float values are not exatcly the expected one
      if (abs(value - rangesValues[currentSegment]) < 0.0001)
      {
        currentSegment++;
        drawLeftButton("-" + String(incValues[currentSegment - 1], decimals), WHITE, RED);
        drawRightButton("+" + String(incValues[currentSegment - 1], decimals), WHITE, BLUE);
      }
      printTftValue(value, 236, 174, 2, decimals);
    }
    
    //TODO: Allow coninuous increasing by keeping pressed the button
    if (leftButton.fell()) {
      printTftValue(value, 236, 174, 2, decimals, BLACK);
      
      value -= incValues[currentSegment - 1];
      if (abs(value -  rangesValues[rangesNum] - incValues[currentSegment - 1]) < 0.0001) {
        printTftValue(value, 236, 174, 2, decimals, BLACK);
        value = rangesValues[0];
        currentSegment = 1;
        drawLeftButton("-" + String(incValues[currentSegment - 1], decimals), WHITE, RED);
        drawRightButton("+" + String(incValues[currentSegment - 1], decimals), WHITE, BLUE);
        //Meter aquí el "- " y mismo decrecimiento
      }
      //Sometimes float values are not exatcly the expected one
      if (abs(value - rangesValues[currentSegment]) < 0.0001)
      {
        currentSegment++;
        drawLeftButton("+" + String(incValues[currentSegment - 1], decimals), WHITE, BLUE);
      }
      if (value < 0 || value == 0) {
        value = 0;
      }
      printTftValue(value, 236, 174, 2, decimals);
    }

    //Change to Calibrate execution
    if (cenButton.fell()) {
      //Deleting explanation
      tft.fillRect(0, 60, 320, 240, BLACK);

      submenu = 1;
      exitFlag = true;
    }
    //Waiting the red button push to start calibration process
    cenButton.update();
    rightButton.update();
    leftButton.update();

  }
  return value;
}

bool yesNoDialog(String message, float x, float y) {
  return yesNoDialog(message, x, y, 2);
}
bool yesNoDialog(String message, float x, float y, int fontSize)
{
  bool answer = false;
  int len = message.length();
  unsigned int w = 6 * fontSize * len;
  unsigned int h = 8 * fontSize;
  uint16_t textBackRect[w * h];
  tft.readRect(x, y, w, h, textBackRect);

  uint16_t redBackRect[78 * 36];
  tft.readRect(242, 206, 78, 36, redBackRect);
  drawRightButton("No");

  uint16_t blueBackRect[78 * 36];
  tft.readRect(0, 206, 78, 36, blueBackRect);
  drawLeftButton("Yes");

  printTftText(message, x, y, RED);
  leftButton.update();
  while ( !leftButton.fell() && !rightButton.fell() )
  {
    leftButton.update();
    rightButton.update();
  }
  printTftText(message, x, y, BLACK);
  tft.writeRect(x, y, w, h, textBackRect);
  tft.writeRect(242, 206, 78, 36, redBackRect);
  tft.writeRect(0, 206, 78, 36, blueBackRect);
  answer = leftButton.fell();
  leftButton.update();
  rightButton.update();
  return answer ;
}

unsigned int selectNextItem (int currentExerciseType, int arrayElements)
{
  currentExerciseType = (currentExerciseType + 1) % arrayElements;
  return currentExerciseType;
}

unsigned int selectPreviousItem (int currentExerciseType, int arrayElements)
{
  currentExerciseType = (currentExerciseType - 1 + arrayElements) % arrayElements;
  return currentExerciseType;
}
