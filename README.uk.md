# AUREON

[English](README.md) | [Українська](README.uk.md)

**Сучасний музичний програвач для Windows, створений на C# та .NET 8.**

AUREON — це настільний музичний програвач, розроблений для комфортного прослуховування музики, з можливостями обробки аудіо, керування плейлистами та сучасним інтерфейсом.

## Можливості

- Підтримка аудіоформатів MP3, FLAC, WAV, AIFF, WMA, M4A, AAC та MP4.
- Керування плейлистами.
- Аудіоеквалайзер.
- Регулювання швидкості відтворення.
- Випадкове та повторне відтворення.
- Регулювання гучності та балансу звуку.
- Візуалізація аудіоспектра.
- Плавні переходи між композиціями.
- Інтеграція музичних сервісів за допомогою WebView2.

## Знімки екрана

### Головне вікно

<img width="346" height="649" alt="Головне вікно AUREON" src="https://github.com/user-attachments/assets/13b46bd2-f702-4601-836b-78a474bed56f" />

### Плейлист

<img width="325" height="169" alt="Плейлист AUREON" src="https://github.com/user-attachments/assets/ab4b89d1-0119-42a3-ac16-f150992ff8fd" />

### Еквалайзер

<img width="340" height="135" alt="Еквалайзер AUREON" src="https://github.com/user-attachments/assets/75c91b12-6960-442a-9a89-913a525b6745" />

### Компактний режим

<img width="346" height="137" alt="Компактний режим AUREON" src="https://github.com/user-attachments/assets/fcc81184-a54e-4ae2-b9fe-07522e44ed8f" />

## Використані технології

- C#
- .NET 8
- Windows Forms
- NAudio
- SoundTouch
- TagLibSharp
- Microsoft Edge WebView2

## Системні вимоги

- Windows 10 або Windows 11 (x64).
- Microsoft Edge WebView2 Runtime.
- Для версій із залежністю від платформи потрібен .NET 8 Desktop Runtime.

## Завантаження

Завантажити останню версію програми можна в розділі [GitHub Releases](../../releases).

## Збирання з вихідного коду

1. Встановіть Visual Studio з робочим навантаженням **Розробка класичних програм .NET**.
2. Клонуйте цей репозиторій:

   ```bash
   git clone https://github.com/CapitanSashko/AUREON.git
   ```

3. Відкрийте файл `AUREON.sln` у Visual Studio.
4. Відновіть пакети NuGet.
5. Зберіть проєкт у конфігурації **Release**.

## Ліцензія

AUREON розповсюджується за ліцензією MIT. Докладніше — у файлі [LICENSE](https://github.com/CapitanSashko/AUREON/blob/main/MP3/LICENSE).

## Автор

Розробник: **Олександр Ечкенко**.
