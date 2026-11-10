# Gamepad Rumble Bridge

Фоновый мост вибрации для нескольких одинаковых HID-геймпадов (VID_0079 PID_0006).

## Возможности

- Каждый геймпад виден в системе как отдельный **Xbox 360 Controller**
- Индивидуальная вибрация через XInputSetState(n)
- Поддержка Steam Input с независимыми настройками
- Named Pipe для ручного управления
- Горячая клавиша Ctrl+Shift+R

## Требования

- Windows 10/11
- .NET 8 Runtime
- **ViGEmBus драйвер** (обязательно!)

## Установка

```powershell
# 1. Установите ViGEmBus (требует прав администратора)
winget install ViGEmBus

# 2. Перезагрузите систему
# 3. Запустите мост от имени администратора
#    Right-click -> RunBridge.bat -> "Run as administrator"
```

## Быстрый старт

```powershell
# Запуск моста
RunBridge.bat

# Тест вибро-маршрутизации (отдельные слоты)
powershell -File SendVibrate.ps1 "0 255 255"  # геймпад 1
powershell -File SendVibrate.ps1 "1 255 255"  # геймпад 2
powershell -File SendVibrate.ps1 "255 255"    # все сразу
powershell -File SendVibrate.ps1 "stop"       # стоп
```

## Как это работает

Мост создаёт по одному виртуальному Xbox 360 контроллеру ViGEm на каждый физический геймпад. Все используют стандартный PID 0x028E. ViGEm автоматически назначает уникальные серийные номера, поэтому Windows видит их как **разные устройства**.

| Слот | XInput Slot | Виртуальный PID |
|------|-------------|-----------------|
| 0    | 0           | 0x028E (Xbox 360) |
| 1    | 1           | 0x028E (Xbox 360) |
| 2    | 2           | 0x028E (Xbox 360) |
| 3    | 3           | 0x028E (Xbox 360) |

Когда игра вызывает `XInputSetState(n)`, мост маршрутизирует вибрацию только в n-й подключённый геймпад.

```
Игра (XInputSetState n)
        ↓
   XInput API
        ↓
   ViGEm Controller[n]
        ↓
 FeedbackReceived (UserIndex=n)
        ↓
   Мост маршрутизирует
        ↓
   HID Report → Геймпад[n]
```

## Настройка в Steam

1. Запустите мост (`RunBridge.bat` от администратора)
2. Откройте Steam → Settings → Controller
3. В игре: правая кнопка → Manage → Controller Layout → Vibration
4. Каждый геймпад настраивается независимо!

## Горячие клавиши

- **Ctrl+Shift+R** — вкл/выкл вибрацию на всех геймпадах
- Double-click по иконке в трее — тоже переключает

## Named Pipe

`\\.\pipe\GamepadRumblePipe` — команды:
- `0 255 255` — вибрация слот 0
- `1 255 255` — вибрация слот 1
- `255 255` — все слоты
- `stop` — стоп все

## Траблшутинг

| Проблема | Решение |
|----------|---------|
| Нет Xbox контроллеров в системе | Установите ViGEmBus: `winget install ViGEmBus`, перезагрузитесь, запустите от администратора |
| Вибрируют все сразу | Проверьте что геймпады подключены по одному, первый = слот 0 |
| Steam показывает Generic | В Steam Settings → Controller отключите "Enable Steam Input for generic controllers" |
| Мост не запускается | Запустите от имени администратора (нужно для ViGEmBus) |