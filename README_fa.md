# Win NCSI Fix

ابزار حرفه‌ای رفع مشکل تشخیص اینترنت ویندوز

🌍 زبان:
[🇺🇸 English](README_en.md) | 🇮🇷 فارسی

## دانلود
آخرین نسخه:
https://github.com/TheGreatAzizi/win-ncsi-fix/releases/latest

صفحه ریلیزها:
https://github.com/TheGreatAzizi/win-ncsi-fix/releases

## این برنامه چیست؟
در ویندوز بخشی به نام NCSI وجود دارد که بررسی می‌کند آیا سیستم به اینترنت متصل است یا خیر.

گاهی به دلیل:
- فیلترینگ
- DNS سفارشی
- VPN
- فایروال
- محدودیت‌های شبکه

ویندوز به اشتباه پیام‌های زیر را نمایش می‌دهد:
- No Internet
- Limited Connectivity

در حالی که اینترنت کاملاً فعال است.

این ابزار به صورت ایمن این مشکل را برطرف می‌کند.

## امکانات
✅ رابط کاربری کنسولی زیبا
✅ اجرای خودکار با دسترسی Administrator
✅ تهیه بکاپ قبل از هر تغییر
✅ بازیابی تنظیمات قبلی
✅ بخش عیب‌یابی
✅ خروجی JSON
✅ پشتیبانی از Probe سفارشی
✅ بازگردانی تنظیمات پیش‌فرض ویندوز
✅ راه‌اندازی مجدد سرویس NLA
✅ سیستم لاگ

## نصب
آخرین نسخه را از لینک زیر دانلود کنید:
https://github.com/TheGreatAzizi/win-ncsi-fix/releases/latest

سپس اجرا کنید:
```powershell
WinNcsiFix.exe
```

## دستورات
مشاهده وضعیت:
```powershell
WinNcsiFix.exe status
```

غیرفعال کردن NCSI:
```powershell
WinNcsiFix.exe disable
```

فعال کردن NCSI:
```powershell
WinNcsiFix.exe enable
```

عیب‌یابی:
```powershell
WinNcsiFix.exe diagnose
```

## امنیت
این برنامه فقط کلید زیر را تغییر می‌دهد:
```reg
HKLM\SYSTEM\CurrentControlSet\Services\NlaSvc\Parameters\Internet
```
قبل از هر تغییر، نسخه پشتیبان تهیه می‌شود.

## توسعه‌دهنده
محمدمهدی عزیزی
GitHub: https://github.com/TheGreatAzizi
Telegram: https://t.me/luluch_code
X/Twitter: https://x.com/the_azzi
