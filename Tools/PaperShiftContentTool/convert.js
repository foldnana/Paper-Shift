#!/usr/bin/env node
"use strict";

const fs = require("fs");
const path = require("path");
const parser = require("./parser.js");

const input = process.argv[2];
const explicitOutput = process.argv[3];

if (!input) {
  console.error("Usage:");
  console.error("  node convert.js input.pspack [output.json]");
  console.error("  node convert.js input.json [output.pspack]");
  process.exit(1);
}

const sourcePath = path.resolve(input);
const source = fs.readFileSync(sourcePath, "utf8");
const fromJson = sourcePath.toLowerCase().endsWith(".json");
const output = explicitOutput || (fromJson ? "output/PaperShiftContentPack.pspack" : "output/PaperShiftContentPack.json");
const outputPath = path.resolve(output);

let content = "";
if (fromJson) {
  content = parser.toPspack(source);
} else {
  const result = parser.parse(source);
  if (result.errors.length > 0) {
    console.error("Content pack has errors:");
    result.errors.forEach(error => console.error("  - " + error));
    process.exit(2);
  }

  content = parser.stringify(result.pack);

  if (result.warnings.length > 0) {
    console.warn("Warnings:");
    result.warnings.forEach(warning => console.warn("  - " + warning));
  }
}

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, content, "utf8");

console.log("Wrote " + outputPath);
