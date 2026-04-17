<p align="center">
  <img src="https://i.ibb.co/nsQrjW07/9bbd5859-0ac8-43c3-8a06-92a841d5e70a.png" width="850" alt="VoidStrike Logo">
</p>

<p align="center">
  <img src="12345678.gif" width="600" alt="VoidStrike Elite Banner">
</p>

<h1 align="center"> VOIDSTRIKE </h1>

<p align="center">
  <strong></strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Build-Stable-success?style=for-the-badge&logo=github-actions" alt="Build">
  <img src="https://img.shields.io/badge/Language-C%23_/_WPF-blueviolet?style=for-the-badge&logo=c-sharp" alt="Language">
  <img src="https://img.shields.io/badge/License-Premium-gold?style=for-the-badge" alt="License">
</p>

---

##  Project Overview / نظرة عامة

**VoidStrike Elite** is a premier hybrid security analysis platform designed for advanced penetration testing. Engineered for speed, depth, and precision, it combines a **Multi-Stage Scanning Engine** with a high-end **Cyber-Elite UI**.

---

##  Key Features / المميزات 

<details open>
<summary><b> Advanced Injection Engines (SQLi / XSS)</b></summary>
<br>
- <b>Error-Based Detection:</b> Supports 50+ database patterns (MySQL, MSSQL, Oracle, etc.).<br>
- <b>Time/Boolean Logic:</b> Advanced algorithms for blind extraction.<br>
- <b>Deep ID Brute:</b> Iterative scanning (1-100) for hidden vulnerable parameters.
</details>

##  Getting Started / ابدأ الآن

###  Building from Source / بناء الأداة 
> [!NOTE]
> **:** Due to GitHub's file size limits (25MB), the pre-compiled standalone binary cannot be hosted directly in the repository. Please build it from source using the command below.
> 
> **:** نظرًا لقيود GitHub على حجم الملفات (25 ميجابايت)، لا يمكن رفع ملف الـ EXE الجاهز مباشرة هنا. يرجى بناء الأداة بنفسك باستخدام الأمر التالي:

1. Clone the repository.
2. Open your terminal in the project folder.
3. Run this command to generate the **Standalone EXE**:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=false /p:DebugType=none /p:DebugSymbols=false
   ```
4. Find your binary in: `bin/Release/net8.0-windows/win-x64/publish/`



<details>
<summary><b> Logic & Discovery (BOLA / IDOR)</b></summary>
<br>
- <b>Smart Baseline Comparison:</b> Intelligent detection of broken object-level authorization by comparing response deltas.<br>
- <b>Parameter Mining:</b> Automatically finds hidden inputs like `user_id`, `admin`, `uuid`.
</details>

<details>
<summary><b> JS Secret Hunter & Recon</b></summary>
<br>
- <b>Secret Extraction:</b> Scans JS files for Firebase Tokens, AWS Keys, and API Endpoints.<br>
- <b>Security Headers:</b> Identifies missing CSP, HSTS, and XSS-Protection headers.<br>
- <b>Server Disclosure:</b> Fingerprints backend technology stacks instantly.
</details>

<details>
<summary><b> Professional Cyber UI (Glassmorphism)</b></summary>
<br>
- <b>White Glow Navigation:</b> Elegant "Old-Style" highlights for a premium feel.<br>
- <b>Animated Backdrop:</b> Low-opacity motion background for non-distractive focus.<br>
- <b>Optimized UX:</b> Minimalist, high-performance interface built with WPF.
</details>

---

##  Getting Started / ابدأ الآن

### 1. Installation
The final stable binary is pre-compiled and ready for deployment.
- **Path:** `Build_Final/VoidStrike.exe`
- **Platform:** Windows x64 (Standalone)

### 2. Authentication
| Field | Value |
| :--- | :--- |
| **Username** | `102` |
| **Password** | `102` |

---

##  Interface Preview / واجهة الأداة

<p align="center">
  <img src="https://i.imgur.com/u7oo4er.png" width="400" style="border-radius: 10px; margin-right: 10px;">
  <img src="https://i.imgur.com/Kyn9f3z.png" width="400" style="border-radius: 10px;">
</p>

---

## ⚖️ Legal Disclaimer / إخلاء المسؤولية

> [!IMPORTANT]
> This tool is strictly for **authorized security auditing** and educational purposes only. The developer (**mkaf7h**) accepts no liability for misuse or damage caused by this software. Use it responsibly.

> [!CAUTION]
> هذه الأداة مخصصة **لفحص الأمان المصرح به** والأغراض التعليمية فقط. المطور (**mkaf7h**) غير مسؤول عن أي سوء استخدام أو أضرار ناتجة. استخدمها بمسؤولية.

---

<p align="center">
  <b>Developed with ❤️ by mkaf7h</b><br>
  <i>"The silence before the strike."</i><br>
  <a href="https://github.com/Mkaf7hDv0">Github</a> • <a href="https://discord.gg/TwRd2ZeE">Discord</a>
</p>
