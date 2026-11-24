# Gamepad Rumble Bridge / Мост Вибрации Геймпадов

![Xbox 360 Controller](https://upload.wikimedia.org/wikipedia/commons/thumb/7/78/Xbox_360_Controller_-_Original.svg/640px-Xbox_360_Controller_-_Original.svg.png)

> **English below / Ниже на русском**

---

## 🇬🇧 English

### Overview

A background Windows service that bridges rumble/vibration from **multiple physical HID gamepads** (Xbox 360-style USB controllers, VID 0079 PID 0006) to **virtual Xbox 360 controllers** via ViGEmBus. Each physical pad gets its own virtual slot so games and Steam can address them individually.

The bridge converts `XInputSetState(n)` calls into HID output reports sent to the correct physical controller, enabling per-gamepad vibration control in any XInput-compatible game or Steam Input configuration.

### Features

- **Multi-pad support** — up to 4 simultaneous HID gamepads, each mapped to an independent Xbox 360 virtual controller
- **Per-pad rumble scaling** — adjust vibration intensity (0–100%) per pad via tray menu → "Pad Settings..."
- **Steam Input compatible** — each virtual controller appears as a separate device in Steam Big Picture mode
- **Named Pipe API** — `\\.\pipe\GamepadRumblePipe` for external control scripts
- **Hotkey** — `Ctrl+Shift+R` toggles all rumble on/off
- **Background tray app** — runs hidden, minimal resource usage

### Supported Hardware

| Type | Vendor ID | Product ID | Notes |
|------|-----------|------------|-------|
| Xbox 360 Wired Controller | `0x0079` | `0x0006` | Original Microsoft controller |
| Xbox 360 Wireless Receiver | `0x0079` | `0x0006` | Any receiver dongle |
| Clone / Third-party pads | `0x0079` | `0x0006` | Most generic USB gamepads |

**These are the only VID/PID combinations currently supported.** If your controller has a different VID/PID, it will not be detected.

### How It Works

```
┌─────────────────────────────────────────────────────┐
│                  GAME / STEAM                        │
│         XInputSetState(0) → left motor ON           │
│         XInputSetState(1) → right motor ON          │
└──────────────────┬──────────────────────────────────┘
                   ↓ XInput API
┌──────────────────▼──────────────────────────────────┐
│              ViGEmBus Virtual Controllers            │
│   Slot 0 ──► Xbox 360 Virtual #1 (UI=0)             │
│   Slot 1 ──► Xbox 360 Virtual #2 (UI=1)             │
│   Slot 2 ──► Xbox 360 Virtual #3 (UI=2)             │
│   Slot 3 ──► Xbox 360 Virtual #4 (UI=3)             │
└──────────────────┬──────────────────────────────────┘
                   ↓ FeedbackReceived events
┌──────────────────▼──────────────────────────────────┐
│              Gamepad Rumble Bridge                   │
│   Routes each event to the matching HID pad          │
│   Applies per-pad rumble scale factor                │
└──────────────────┬──────────────────────────────────┘
                   ↓ HID Output Reports
┌──────────────────▼──────────────────────────────────┐
│            Physical HID Gamepads                     │
│   Pad #1 ←─── XInput[0]  (VID_0079&PID_0006)        │
│   Pad #2 ←─── XInput[1]  (VID_0079&PID_0006)        │
└─────────────────────────────────────────────────────┘
```

### Per-Pad Rumble Scaling

Open the tray menu → **"Pad Settings..."** to configure:
- **Scale 100%** — full rumble (default, recommended)
- **Scale 50%** — half-intensity rumble
- **Scale 0%** — disable rumble for this pad

Scaling applies to ALL rumble sources: game XInput calls and manual pipe commands.

### Project File Structure

```
GamepadBridge/
├── src/
│   ├── GamepadRumbleBridge/        # Main bridge application
│   │   ├── Program.cs              # Core logic (HID monitor, ViGEm routing, pipe server)
│   │   ├── PadSettingsForm.cs      # Pad settings dialog (rumble scale per slot)
│   │   ├── PadSettingsForm.Designer.cs
│   │   ├── Nefarius.ViGEm.Client.dll  # ViGEm client library
│   │   ├── app.manifest            # Admin privilege requirement
│   │   └── GamepadRumbleBridge.csproj
│   └── DebugProbe/                 # Test utility (XInput + ViGEm debugging)
│       └── Program.cs
├── dist/                           # Ready-to-run distribution
│   ├── GamepadRumbleBridge.exe     # Main executable (apphost)
│   ├── GamepadRumbleBridge.dll     # Managed assembly with bridge code
│   ├── Nefarius.ViGEm.Client.dll   # ViGEm runtime DLL
│   ├── RunBridge.bat               # Launcher (requires admin)
│   ├── SendVibrate.ps1             # Manual rumble test script
│   ├── AddToStartup.vbs            # Auto-start helper
│   ├── README.md                   # This file
│   └── CHANGELOG.md                # Version history
└── .gitignore
```

### Installation

#### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8 Runtime** — [download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **ViGEmBus driver** — required! Install once:

  ```powershell
  # From elevated PowerShell:
  winget install ViGEmBus
  # Then REBOOT the system
  ```

#### Steps

1. Download the latest release ZIP from GitHub
2. Extract to any folder (e.g., `C:\Program Files\GamepadRumbleBridge`)
3. Right-click **`RunBridge.bat`** → **"Run as administrator"**
4. The bridge icon appears in the system tray
5. Plug in your HID gamepads — they auto-detect within ~3 seconds

### Usage

#### Tray Menu

| Item | Action |
|------|--------|
| Toggle Vibration (Ctrl+Shift+R) | Enable/disable rumble on all pads |
| Status: x2 – OK | Shows connected count and state |
| **Pad Settings...** | Open per-pad rumble scaling dialog |
| Exit | Stop the bridge and disconnect all pads |

#### Named Pipe Commands

Connect to `\\.\pipe\GamepadRumblePipe`:

```powershell
# Via PowerShell
powershell -File SendVibrate.ps1 "0 255 255"    # Pad 0, both motors full
powershell -File SendVibrate.ps1 "1 255 0"      # Pad 1, left motor only
powershell -File SendVibrate.ps1 "255 255"      # All pads, both motors
powershell -File SendVibrate.ps1 "stop"         # Stop all rumble
```

#### Steam Input Setup

1. Launch the bridge (`RunBridge.bat` as admin)
2. Open Steam → **Settings → Controller**
3. In-game: right-click → **Manage → Controller Layout → Vibration**
4. Select individual controllers — each is independently configurable
5. Test: use Steam's built-in vibration test in each controller's layout editor

### Troubleshooting

| Problem | Solution |
|---------|----------|
| No Xbox controllers appear | Run as admin; ensure ViGEmBus is installed and system was rebooted |
| Only 1 controller shows in Steam | Restart the bridge; check Device Manager for multiple Xbox entries |
| All pads rumble together | Replug pads one at a time; first plug = slot 0 |
| Generic name instead of Xbox 360 | Steam Settings → Controller → uncheck "Enable Steam Input for generic controllers" |
| Bridge won't start | Must run as Administrator (ViGEmBus requires admin) |
| Pad stops responding | Unplug and replug; bridge re-detects every 3 seconds |

---

## 🇷🇺 Русский

### Описание

Фоновое приложение Windows, которое маршрутизирует вибрацию (румбл) от **нескольких физических HID-геймпадов** (USB контроллеры типа Xbox 360, VID 0079 PID 0006) в **виртуальные Xbox 360 контроллеры** через драйвер ViGEmBus. Каждый физический геймпад получает собственный виртуальный слот, поэтому игры и Steam могут управлять вибрацией каждого независимо.

Мост преобразует вызовы `XInputSetState(n)` в HID-отчёты, отправляемые в правильный физический геймпад, обеспечивая индивидуальную вибро-маршрутизацию в любой игре с поддержкой XInput и в конфигурациях Steam Input.

### Возможности

- **Мультигеймпэд** — до 4 одновременных HID-геймпадов, каждый в своём виртуальном слоте Xbox 360
- **Настройка масштаба вибро** — регулируйте интенсивность (0–100%) для каждого геймпада через меню трэя → **"Настройки геймпадов..."**
- **Совместимость с Steam Input** — каждый виртуальный контроллер виден как отдельное устройство в Steam Big Picture
- **Named Pipe API** — `\\.\pipe\GamepadRumblePipe` для внешних скриптов управления
- **Горячая клавиша** — `Ctrl+Shift+R` включает/выключает вибро на всех геймпадах
- **Фоновое приложение** — работает скрыто в системном трее, минимальное потребление ресурсов

### Поддерживаемое оборудование

| Тип | Vendor ID | Product ID | Примечание |
|-----|-----------|------------|------------|
| Xbox 360 Проводной контроллер | `0x0079` | `0x0006` | Оригинальный Microsoft |
| Xbox 360 Беспроводной приёмник | `0x0079` | `0x0006` | Любой USB-приёмник |
| Клоны / универсальные.pad'ы | `0x0079` | `0x0006` | Большинство USB-геймпадов |

**Поддерживаются только комбинации VID/PID = 0079:0006.** Если ваш контроллер имеет другой VID/PID, он не будет обнаружен.

### Как это работает

```
┌─────────────────────────────────────────────────────┐
│                    ИГРА / STEAM                      │
│         XInputSetState(0) → левый мотор ВКЛ         │
│         XInputSetState(1) → правый мотор ВКЛ        │
└──────────────────┬──────────────────────────────────┘
                   ↓ API XInput
┌──────────────────▼──────────────────────────────────┐
│           Виртуальные контроллеры ViGEmBus           │
│   Слот 0 ──► Виртуальный Xbox 360 #1 (UI=0)         │
│   Слот 1 ──► Виртуальный Xbox 360 #2 (UI=1)         │
│   Слот 2 ──► Виртуальный Xbox 360 #3 (UI=2)         │
│   Слот 3 ──► Виртуальный Xbox 360 #4 (UI=3)         │
└──────────────────┬──────────────────────────────────┘
                   ↓ События FeedbackReceived
┌──────────────────▼──────────────────────────────────┐
│              Мост Вибрации Геймпадов                 │
│   Маршрутизирует каждое событие в нужный HID-геймпад │
│   Применяет коэффициент масштабирования вибро       │
└──────────────────┬──────────────────────────────────┘
                   ↓ HID Output Reports
┌──────────────────▼──────────────────────────────────┐
│              Физические HID-геймпады                 │
│   Геймпад #1 ←─── XInput[0]  (VID_0079&PID_0006)    │
│   Геймпад #2 ←─── XInput[1]  (VID_0079&PID_0006)    │
└─────────────────────────────────────────────────────┘
```

### Настройка масштаба вибро по геймпадам

Откройте меню трэя → **"Настройки геймпадов..."** для настройки:
- **Масштаб 100%** — полная вибрация (по умолчанию, рекомендуется)
- **Масштаб 50%** — вибрация половинной силы
- **Масштаб 0%** — отключить вибро для этого геймпада

Масштабирование применяется ко ВСЕМ источникам вибро: игровым вызовам XInput и ручным командам через pipe.

### Структура файлов проекта

```
GamepadBridge/
├── src/
│   ├── GamepadRumbleBridge/        # Основное приложение моста
│   │   ├── Program.cs              # Основная логика (HID-монитор, ViGEm, pipe-сервер)
│   │   ├── PadSettingsForm.cs      # Диалог настроек геймпадов (масштаб вибро)
│   │   ├── PadSettingsForm.Designer.cs
│   │   ├── Nefarius.ViGEm.Client.dll  # Библиотека ViGEm
│   │   ├── app.manifest            # Требование прав администратора
│   │   └── GamepadRumbleBridge.csproj
│   └── DebugProbe/                 # Утилита тестирования (отладка XInput + ViGEm)
│       └── Program.cs
├── dist/                           # Готовая к запуску сборка
│   ├── GamepadRumbleBridge.exe     # Главный исполняемый файл (apphost)
│   ├── GamepadRumbleBridge.dll     # Управляемая сборка с кодом моста
│   ├── Nefarius.ViGEm.Client.dll   #_runtime DLL ViGEm
│   ├── RunBridge.bat               # Запускатель (требует админа)
│   ├── SendVibrate.ps1             # Скрипт ручного теста вибро
│   ├── AddToStartup.vbs            # Помощник автозапуска
│   ├── README.md                   # Этот файл
│   └── CHANGELOG.md                # История версий
└── .gitignore
```

### Установка

#### Требования

- **Windows 10/11** (64-bit)
- **.NET 8 Runtime** — [скачать здесь](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Драйвер ViGEmBus** — обязателен! Установите один раз:

  ```powershell
  # Из PowerShell ОТ ИМЕНИ АДМИНИСТРАТОРА:
  winget install ViGEmBus
  # Затем ПЕРЕЗАГРУЗИТЕ систему
  ```

#### Шаги

1. Скачайте последний ZIP-релиз с GitHub
2. Распакуйте в любую папку (например, `C:\Program Files\GamepadRumbleBridge`)
3. Правой кнопкой по **`RunBridge.bat`** → **"Запуск от имени администратора"**
4. В системном трее появится значок моста
5. Подключите HID-геймпады — они автоматически обнаружатся за ~3 секунды

### Использование

#### Меню трэя

| Пункт | Действие |
|-------|----------|
| Toggle Vibration (Ctrl+Shift+R) | Вкл/выкл вибро на всех геймпадах |
| Status: x2 – OK | Показывает количество подключённых геймпадов |
| **Pad Settings...** | Открыть диалог настройки масштаба вибро |
| Exit | Остановить мост и отключить все геймпады |

#### Команды через Named Pipe

Подключитесь к `\\.\pipe\GamepadRumblePipe`:

```powershell
# Через PowerShell
powershell -File SendVibrate.ps1 "0 255 255"    # Геймпад 0, оба мотора на полную
powershell -File SendVibrate.ps1 "1 255 0"      # Геймпад 1, только левый мотор
powershell -File SendVibrate.ps1 "255 255"      # Все геймпады, оба мотора
powershell -File SendVibrate.ps1 "stop"         # Остановить всю вибрацию
```

#### Настройка в Steam Input

1. Запустите мост (`RunBridge.bat` от администратора)
2. Откройте Steam → **Настройки → Контроллер**
3. В игре: правая кнопка мыши → **Управление → Схема контроллера → Вибрация**
4. Выберите отдельные контроллеры — каждый настраивается независимо
5. Тест: используйте встроенный тест вибрации в редакторе схемы контроллера

### Устранение неполадок

| Проблема | Решение |
|----------|---------|
| Не появляются Xbox-контроллеры | Запустите от администратора; убедитесь что ViGEmBus установлен и система перезапущена |
| В Steam виден только 1 контроллер | Перезапустите мост; проверьте Диспетчер устройств на наличие нескольких Xbox |
| Вибрируют все сразу | Переподключайте геймпады по одному; первый включённый = слот 0 |
| Вместо Xbox 360 отображается Generic | Steam → Настройки → Контроллер → уберите "Enable Steam Input for generic controllers" |
| Мост не запускается | Необходимо запустить от имени администратора (ViGEmBus требует админ-прав) |
| Геймпад перестал реагировать | Отключите и подключите заново; мост проверяет каждые 3 секунды |

---

## License

MIT License — feel free to modify and redistribute.

---

## Repository

[github.com/ersdgera/GamepadRumbleBridge](https://github.com/ersdgera/GamepadRumbleBridge)
