# Gamepad Rumble Bridge

Фоновый мост вибрации для нескольких одинаковых HID-геймпадов (VID_0079 PID_0006).

## Возможности

- Каждый геймпад виден в системе как отдельный Xbox 360 контроллер
- Индивидуальная вибрация через XInputSetState(n)
- Поддержка Steam Input с независимыми настройками
- Named Pipe для ручного управления
- Горячая клавиша Ctrl+Shift+R

## Как это работает

Мост создаёт по одному виртуальному Xbox 360 контроллеру ViGEm на каждый физический геймпад. Уникальный Product ID (PID) каждого слота обеспечивает разные XInput UserIndex:

| Слот | PID    | XInput Slot |
|------|--------|-------------|
| 0    | 0x028E | 0           |
| 1    | 0x038E | 1           |
| 2    | 0x048E | 2           |
| 3    | 0x058E | 3           |

Когда игра вызывает `XInputSetState(n)`, мост маршрутизирует вибрацию только в n-й подключённый геймпад.

## Быстрый старт

```powershell
# Запуск моста
.\RunBridge.bat

# Тест вибро-маршрутизации (отдельные слоты)
powershell -File SendVibrate.ps1 "0 255 255"  # геймпад 1
powershell -File SendVibrate.ps1 "1 255 255"  # геймпад 2
powershell -File SendVibrate.ps1 "255 255"    # все
powershell -File SendVibrate.ps1 "stop"       # стоп
```

## Требования

- Windows 10/11
- .NET 8 Runtime
- [ViGEmBus драйвер](https://github.com/nefarius/ViGEmBus/releases)
- Игры, поддерживающие XInput

## Настройка в Steam

1. Запустите мост (`RunBridge.bat`)
2. Откройте Steam → Settings → Controller
3. В игре: правая кнопка → Manage → Controller Layout → Vibration

## Архитектура

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
