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




