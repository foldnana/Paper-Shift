# Paper Shift 内容编辑器

这是一个 Unity 外部的小工具，用来快速编辑、导入、导出标签、工作标签、事件、公司岗位、后半生规则等内容。

## 打开方式

直接用浏览器打开：

```text
Tools/PaperShiftContentTool/index.html
```

它不需要 npm，不需要启动服务器。

## 文本转 JSON

浏览器里点“下载 JSON”，或使用命令行：

```bash
node Tools/PaperShiftContentTool/convert.js Tools/PaperShiftContentTool/sample.pspack Tools/PaperShiftContentTool/output/PaperShiftContentPack.json
```

## JSON 转文本包

在浏览器右侧粘贴 JSON 后点“JSON 转文本”，或导入 `.json` 文件。也可以使用命令行：

```bash
node Tools/PaperShiftContentTool/convert.js Tools/PaperShiftContentTool/output/PaperShiftContentPack.json Tools/PaperShiftContentTool/output/PaperShiftContentPack.pspack
```

## 导入 Unity

1. 在 Unity 里执行 `Paper Shift / Content Tool / Import Content Pack JSON...`。
2. 选择工具导出的 `PaperShiftContentPack.json`。
3. Unity 会生成或更新 `Assets/PaperShift/Data/PaperShiftImportedDatabase.asset`。
4. 把这个数据库资产拖到场景里的 `PaperShiftGamePresenter.Database`。

也可以执行 `Paper Shift / Content Tool / Assign Imported Database To Open Scene`，把导入后的数据库赋给当前打开场景里的 Presenter。

## 给 AI 写内容

详细规则见：

```text
Docs/PaperShiftContentAuthoring.md
```
