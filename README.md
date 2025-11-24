![Avalonia](https://img.shields.io/badge/Avalonia-9B4F96?style=flat&logo=avalonia&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=flat&logo=csharp&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-003B57?style=flat&logo=sqlite&logoColor=white)
![ONNX Runtime](https://img.shields.io/badge/ONNX_Runtime-005CED?style=flat&logo=onnx&logoColor=white)
![HuggingFace](https://img.shields.io/badge/HuggingFace-FFD21E?style=flat&logo=huggingface&logoColor=black)

## AI-Enhanced Notes Manager

A cross-platform note management tool built on Avalonia.
It offers rich-text editing, customizable topic metadata, fast hierarchical filtering, semantic AI topic suggestions, and a local SQLite backend.
Designed for users who want a structured, intelligent, and fully offline way to manage personal knowledge.

<img width="1264" height="759" alt="CustomizableTextBox" src="https://github.com/user-attachments/assets/c89a6ea0-cbd5-485e-a56c-4c1f4ce38799" />

##### Create, edit, and manage all your notes in one flexible rich-text workspace.

#### Customizable Topics for Better Organization

<img width="380" height="331" alt="image" src="https://github.com/user-attachments/assets/79b67592-c9ec-47b3-aedf-efe34a0760a1" />

##### Create your own categories, tag notes instantly, and shape your knowledge structure.

## Features
* Rich-text note editor with formatting tools (bold, italic, underline, colors, fonts, sizes).

* AI-powered topic suggestions based on your note content.

* Custom topic creation with unlimited categories.

* Smart search (by title or full content).

* Topic-based filtering to quickly narrow your notes.

* Similar notes detection to automatically surface related information.

* Instant save and update tracking with timestamps.

* Fast, cached data access powered by SQLite.

* Clean, modern UI with a focus on productivity.

* Fully offline — your data stays on your machine.

## Model Download



This project uses the **e5-base-v2** embedding model for topic prediction.

Due to size limits, the model is not included in the repository.



### Download the ONNX model from HuggingFace:



    => https://huggingface.co/intfloat/e5-base-v2/tree/main/onnx



After downloading, place the model file here:

Nexus.Core/AI/e5-base-v2/





Your folder structure should look exactly:

```

Nexus.Core/
    ---- AI/
        ---- e5-base-v2/
            ---- model.onnx

```



otherwise, the application would crash on startup.



## Rich Text Editing (AvRichTextBox – Customized Fork)

This project uses a customized fork of the **AvRichTextBox library**, originally created by  
[cuikp](https://github.com/cuikp/AvRichTextBox).

My fork is available [here](https://github.com/artiomAdam/AvRichTextBox).

This fork includes structural and behavioral changes, so it is not a drop-in replacement for the
original library. All credit for the original implementation goes to **cuikp**.




