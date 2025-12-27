# N2K-Toolkit
A whole bunch of Toolkit that I'm currently using in real projects.

---

## Features
- Design Patterns

| Name | Description |
|-----|------------|
| **Singleton Pattern** | Easy-to-use base class for singleton `MonoBehaviour`s. |
| **UI Manager** | Centralized UI control system. |
| **Object Pooling** | Performance-optimized object reuse system. |
| **Animator Controller** | Simplified animator state management. |
| **Object Logger** | Attachable logger component for `MonoBehaviour` runtime debugging. |

- Tools

| Name | Description |
|-----|------------|
| **Prefab Saver** | Quickly save a `GameObject` into a prefab while preserving its state for prototyping. |
| **Create Empty Parent At Child Pos** | Menu item to create an empty parent object at the child’s position. |
| **ReadOnly Attribute** | Custom editor attribute to display values in the Inspector as read-only. |
| **InfoBox Attribute** | Custom editor attribute to display information boxes in the Inspector for notes or messages. |

## Unity version supported

Unity 2021.1 and newer.

## Installation
1. Download `N2K_Toolkit_Vx.x.x.unitypackage` from [lastest release](https://github.com/Khanh-N2K/N2K-Toolkit/releases/tag/V1.0.6).
2. In Unity, go to `Assets -> Import Package -> Custom Package...`.
3. Select the file and import.

## Usage
### Singleton
Simply add to the class you want to be singleton.
  
``` csharp
public class UIManager : Singleton<UIManager>
{
  protected override bool IsDontDestroyOnLoad => false;   // Override IsDontDestroyOnLoad

  protected override void OnSingletonAwake()
  {
    // Implement abstract OnSingletonAwake()
  }

  // Your remaining class here
}
```

### Remaining
This part will be updated after I graduated at HUST. Please waiting...

## License
[MIT License © 2025 Khanh.N2K](https://github.com/Khanh-N2K/N2K-Toolkit/blob/main/LICENSE).
