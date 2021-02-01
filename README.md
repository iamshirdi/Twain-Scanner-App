# Twain Windows API (sample library implementation) 
- I have recreated (a sample working) version of netmaster twain in windows forums
https://www.codeproject.com/Articles/1376/NET-TWAIN-image-scanner


<br>

<p align="center">
  <img width="460" height="300" src="twain scanner screenshot.png">
</p>
<br>

## Twain scanner WinForms App 
- Sample Twain windows forum App
- Created using Twain32.dll found default in Windows system32 (supports twain compatibility layer to control wia drivers:not foolproof errors follow)
- It raises driver gui interface (no advanced programmatic negotiaion implementation
- gets data from intptr handle(memory) and convert to bitmap object for further processing
- save using file dialog
- Refer todo notes for pending changes/implementations




### Please check Twain docummentation this is not your go to documentation
For better understanding as starters refer below link. 
https://www.codeproject.com/Articles/991207/TWAINComm-A-Csharp-TWAIN-Communications-Library
