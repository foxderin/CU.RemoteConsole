// ── Build script: assemble web/src/index.html from template + JS includes ──
import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const templatePath = path.join(root, "web/src/index.html.template");
const outputPath = path.join(root, "web/src/index.html");

let html = fs.readFileSync(templatePath, "utf8");

// Replace <!--INCLUDE path/to/file--> markers with file content
html = html.replace(/<!--INCLUDE\s+([\w./-]+)-->/g, (match, includePath) => {
  const filePath = path.join(root, "web/src", includePath);
  if (!fs.existsSync(filePath)) {
    console.warn(`Warning: Include file not found: ${filePath}`);
    return `/* Missing: ${includePath} */`;
  }
  return fs.readFileSync(filePath, "utf8");
});

fs.writeFileSync(outputPath, html);
console.log(`Assembled ${outputPath}`);
