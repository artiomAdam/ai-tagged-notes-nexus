\## Model Download



This project uses the \*\*e5-base-v2\*\* embedding model for topic prediction.

Due to size limits, the model is not included in the repository.



\###Download the ONNX model from HuggingFace:



&nbsp;=> https://huggingface.co/intfloat/e5-base-v2/tree/main/onnx



After downloading, place the model file here:

Nexus.Core/AI/e5-base-v2/





Your folder structure should look exactly:

```

Nexus.Core/

&nbsp;---- AI/

&nbsp;    ---- e5-base-v2/

&nbsp;        ---- model.onnx

```





This ensures the application can load the model at runtime.

