import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";

const root = path.resolve(path.dirname(new URL(import.meta.url).pathname), "..");
const srcPptx = "/tmp/next-template/pptx";
const work = "/tmp/next-template/filled";
const assets = "/Users/markmamenko/.cursor/projects/Users-markmamenko-Projects-CodeSensei/assets";
const outDir = path.join(root, "docs/presentation");
const outPptx = path.join(outDir, "CodeSensei-NEXT-presentation.pptx");

fs.rmSync(work, { recursive: true, force: true });
execFileSync("cp", ["-R", srcPptx, work]);
fs.mkdirSync(path.join(work, "ppt/media"), { recursive: true });
fs.mkdirSync(outDir, { recursive: true });

const files = {
  metalab: path.join(assets, "metalab-interior.png"),
  campus: path.join(assets, "kpi-campus.png"),
  vr: path.join(assets, "vr-terminal.png"),
  terminal: path.join(assets, "artefact-terminal.png"),
  presets: path.join(assets, "artefact-presets.png"),
  paste: path.join(assets, "artefact-paste.png"),
  qrGithub: "/tmp/next-template/qr-github.png",
  qrPaste: "/tmp/next-template/qr-paste.png",
};

for (const [name, src] of Object.entries(files)) {
  fs.copyFileSync(src, path.join(work, `ppt/media/${name}.png`));
  fs.copyFileSync(src, path.join(outDir, `${name}.png`));
}

function readSlide(n) {
  return fs.readFileSync(path.join(work, `ppt/slides/slide${n}.xml`), "utf8");
}
function writeSlide(n, xml) {
  fs.writeFileSync(path.join(work, `ppt/slides/slide${n}.xml`), xml);
}
function replaceEach(xml, search, values) {
  let i = 0;
  return xml.replaceAll(search, () => {
    const v = values[i] ?? search;
    i += 1;
    return v;
  });
}

function pic({ id, name, rId, x, y, cx, cy }) {
  return `<p:pic><p:nvPicPr><p:cNvPr id="${id}" name="${name}"/><p:cNvPicPr><a:picLocks noChangeAspect="0"/></p:cNvPicPr><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed="${rId}"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr><a:xfrm><a:off x="${x}" y="${y}"/><a:ext cx="${cx}" cy="${cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr></p:pic>`;
}

function insertPics(xml, picsXml) {
  return xml.replace("</p:spTree>", `${picsXml}</p:spTree>`);
}

function addImageRels(slideNo, rels) {
  const relPath = path.join(work, `ppt/slides/_rels/slide${slideNo}.xml.rels`);
  let xml = fs.readFileSync(relPath, "utf8");
  const extra = rels
    .map(
      ([id, file]) =>
        `<Relationship Id="${id}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="../media/${file}"/>`
    )
    .join("");
  xml = xml.replace("</Relationships>", `${extra}</Relationships>`);
  fs.writeFileSync(relPath, xml);
}

let s1 = readSlide(1);
s1 = s1.replace("[Project Title]", "CodeSensei");
s1 = s1.replace(" [Nomination I / Nomination II]", " Nomination I — Virtual university location");
s1 = s1.replace("[Team Name]", "CodeSensei");
s1 = s1.replace("[University Name]", "Igor Sikorsky Kyiv Polytechnic Institute");
s1 = s1.replace(
  "[Name Surname], [Name Surname, Name Surname…]",
  "Mark Mamenko, Kateryna Shozda, Denys Ilienko, Sviatoslav Pavlenko, Dmytro Poshytyniuk"
);
writeSlide(1, s1);

let s2 = readSlide(2);
s2 = replaceEach(s2, "[Name Surname]", ["Mark Mamenko", "Kateryna Shozda", "Denys Ilienko"]);
s2 = replaceEach(s2, "[Faculty]", [
  "Igor Sikorsky KPI",
  "Igor Sikorsky KPI",
  "Igor Sikorsky KPI",
]);
s2 = replaceEach(s2, "[e.g. 3D modelling, VRChat development, design]", [
  "Team lead. Backend API, Gemini proxy, Render hosting.",
  "Learning design. OOP presets and mentor prompts.",
  "Quality assurance. Tests, API contract, demo checklist.",
]);
s2 = s2.replace(" You may add a second Team slide if needed.", " Continued on the next slide.");
writeSlide(2, s2);

fs.copyFileSync(path.join(work, "ppt/slides/slide2.xml"), path.join(work, "ppt/slides/slide11.xml"));
fs.copyFileSync(
  path.join(work, "ppt/slides/_rels/slide2.xml.rels"),
  path.join(work, "ppt/slides/_rels/slide11.xml.rels")
);

let s11 = fs.readFileSync(path.join(work, "ppt/slides/slide11.xml"), "utf8");
s11 = s11.replace("Our Team", "Our Team (continued)");
s11 = replaceEach(s11, "Mark Mamenko", ["Sviatoslav Pavlenko"]);
s11 = replaceEach(s11, "Kateryna Shozda", ["Dmytro Poshytyniuk"]);
s11 = replaceEach(s11, "Denys Ilienko", ["CodeSensei authors"]);
s11 = replaceEach(s11, "Team lead. Backend API, Gemini proxy, Render hosting.", [
  "VRChat client. UdonSharp terminal, GET-only networking, prefab.",
]);
s11 = replaceEach(s11, "Learning design. OOP presets and mentor prompts.", [
  "World integration. MetaLab assembly, VRChat SDK, scene placement.",
]);
s11 = replaceEach(s11, "Quality assurance. Tests, API contract, demo checklist.", [
  "Authorship of created artefacts remains with the team.",
]);
s11 = s11.replace(" Continued on the next slide.", " Five authors; artefacts remain the team’s IP.");
fs.writeFileSync(path.join(work, "ppt/slides/slide11.xml"), s11);

let s3 = readSlide(3);
s3 = s3.replace("[Name of the building / university location]", "Multimedia Laboratory (MetaLab), Igor Sikorsky KPI");
s3 = s3.replace(
  "[Briefly describe the selected location and its role at the university — max. 2 sentences.]",
  "MetaLab is the university multimedia space established under Erasmus+ NEXT. It is the physical counterpart of the virtual classroom where students practise digital and programming skills."
);
s3 = s3.replace(
  "[List the areas included in your virtual model.]",
  "Student workstation with the CodeSensei terminal; 24 OOP preset buttons; code-review station (5-character ticket + /paste); shared display for collaborative learning."
);
writeSlide(3, s3);

let s4 = readSlide(4);
s4 = s4.replace("[Insert a main photo of the location]", "MetaLab workstation — reference interior");
s4 = s4.replace("[OPTIONAL: Add 1–3 additional reference photos]", "Campus context, Igor Sikorsky KPI");
s4 = insertPics(
  s4,
  pic({ id: 401, name: "MetaLab", rId: "rId10", x: 322800, y: 1459375, cx: 7000000, cy: 4300000 }) +
    pic({ id: 402, name: "Campus", rId: "rId11", x: 7500000, y: 1459375, cx: 4300000, cy: 4300000 })
);
writeSlide(4, s4);
addImageRels(4, [
  ["rId10", "metalab.png"],
  ["rId11", "campus.png"],
]);

let s5 = readSlide(5);
s5 = s5.replace("[Insert a main screenshot of your virtual model]", "CodeSensei terminal in the virtual laboratory");
s5 = s5.replace("[OPTIONAL: Add 1–3 additional screenshots if needed]", "VRChat-compatible GET-only workstation");
s5 = s5.replace("[Add 1–3 additional screenshots if needed]", "UdonSharp terminal with 55-character lines");
s5 = insertPics(
  s5,
  pic({ id: 501, name: "VR model", rId: "rId10", x: 322800, y: 1459375, cx: 11546400, cy: 4300000 })
);
writeSlide(5, s5);
addImageRels(5, [["rId10", "vr.png"]]);

let s6 = readSlide(6);
s6 = replaceEach(s6, "[Artefact name]", [
  "CodeSensei Terminal",
  "OOP preset catalogue",
  "AI code-review inbox",
]);
s6 = replaceEach(s6, "[Insert screenshot / image]", ["", "", ""]);
s6 = replaceEach(s6, "[Briefly describe the artefact and its role in the virtual environment.]", [
  "VRChat prefab: UdonSharp terminal, 5.5 s GET queue, synced answers.",
  "24 OOP topics (encapsulation to OOP vs procedural) on preset buttons.",
  "Student gets a 5-character code, pastes a snippet, Gemini replies on the VR screen.",
]);
s6 = insertPics(
  s6,
  pic({ id: 601, name: "Terminal", rId: "rId10", x: 163275, y: 1870350, cx: 3809400, cy: 3305700 }) +
    pic({ id: 602, name: "Presets", rId: "rId11", x: 4191213, y: 1870350, cx: 3809400, cy: 3305700 }) +
    pic({ id: 603, name: "Paste", rId: "rId12", x: 8219238, y: 1870350, cx: 3809400, cy: 3305700 })
);
writeSlide(6, s6);
addImageRels(6, [
  ["rId10", "terminal.png"],
  ["rId11", "presets.png"],
  ["rId12", "paste.png"],
]);

let s7 = readSlide(7);
s7 = s7.replace(
  "[List the main tools used.]",
  "Unity, VRChat SDK3, UdonSharp, C# / .NET 10, ASP.NET Minimal APIs, Docker, Render, Google Gemini, GitHub, NUnit."
);
s7 = s7.replace(
  "[Briefly describe the main stages of creating the model.]",
  "1) Align the HTTP contract with VRChat GET-only limits. 2) Build the .NET proxy and 24 presets. 3) Package the UdonSharp terminal. 4) Deploy to Render and verify the live inbox."
);
s7 = s7.replace(
  "[Explain how the created assets were integrated and tested in VRChat.]",
  "The prefab uses VRCStringDownloader only. URLs are baked to https://codesensei-d5zi.onrender.com. Players must enable Allow Untrusted URLs. Polling respects the 5.5 s VRChat limit."
);
s7 = s7.replace(
  "[Optional: mention 1–2 key challenges and how you addressed them.]",
  "Udon cannot POST, so review is ticket + /paste + inbox. Render Free sleeps — /health wakes the instance. Pending tickets are hidden so the client does not stop polling early."
);
s7 = s7.replace(
  " You may use additional slides if needed to explain the technical implementation clearly.",
  " Assets follow VRChat SDK3 / UdonSharp and are ready for the NEXT MetaLab world."
);
writeSlide(7, s7);

let s8 = readSlide(8);
s8 = s8.replace(
  "[How can students or teachers use this virtual location?]",
  "In the NEXT-Study Metaverse lab a student presses an OOP preset or starts a live code review. A teacher sees the same synced answer on the shared terminal."
);
s8 = s8.replace(
  "[What can visitors explore, learn or do in this environment?]",
  "Explore 24 OOP explanations, generate a review ticket, paste a snippet on the web form, and read a mentor reply formatted for the VR screen (max 55 characters per line)."
);
s8 = s8.replace(
  "[How could the model be extended or enriched in the future?]",
  "Persistent ticket store, more languages, keep-alive Action, additional lab furniture, and a public VRChat world listing after MetaLab import."
);
writeSlide(8, s8);

let s9 = readSlide(9);
s9 = s9.replace(
  "[Insert a QR code or link to the VRChat environment / video demonstration.]",
  "Live demo: https://codesensei-d5zi.onrender.com/paste   Health: https://codesensei-d5zi.onrender.com/health"
);
s9 = s9.replace(
  "[Add links to the project files, source code, 3D assets or other submitted materials.]",
  "GitHub: https://github.com/mamenkomarkus-creator/CodeSensei   Prefab: client/CodeSensei.unitypackage   API: docs/api.md"
);
s9 = s9.replace(
  "[Add any other relevant links or documentation.]",
  "VRChat: https://hello.vrchat.com/   NEXT: https://nextstudy.eu/   Integration notes: docs/NEXT-INTEGRATION.md"
);
s9 = s9.replace(
  " Make sure all links and QR codes are accessible and working.",
  " All links are public. Authorship of artefacts remains with the team."
);
s9 = insertPics(
  s9,
  pic({ id: 901, name: "QR GitHub", rId: "rId10", x: 9800000, y: 1600000, cx: 1600000, cy: 1600000 }) +
    pic({ id: 902, name: "QR Paste", rId: "rId11", x: 9800000, y: 3400000, cx: 1600000, cy: 1600000 })
);
writeSlide(9, s9);
addImageRels(9, [
  ["rId10", "qrGithub.png"],
  ["rId11", "qrPaste.png"],
]);

let pres = fs.readFileSync(path.join(work, "ppt/presentation.xml"), "utf8");
pres = pres.replace(
  '<p:sldId id="257" r:id="rId7"/>',
  '<p:sldId id="257" r:id="rId7"/><p:sldId id="266" r:id="rId16"/>'
);
fs.writeFileSync(path.join(work, "ppt/presentation.xml"), pres);

let rels = fs.readFileSync(path.join(work, "ppt/_rels/presentation.xml.rels"), "utf8");
rels = rels.replace(
  "</Relationships>",
  '<Relationship Id="rId16" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide11.xml"/></Relationships>'
);
fs.writeFileSync(path.join(work, "ppt/_rels/presentation.xml.rels"), rels);

let ctypes = fs.readFileSync(path.join(work, "[Content_Types].xml"), "utf8");
ctypes = ctypes.replace(
  "</Types>",
  '<Override ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml" PartName="/ppt/slides/slide11.xml"/></Types>'
);
fs.writeFileSync(path.join(work, "[Content_Types].xml"), ctypes);

fs.rmSync(outPptx, { force: true });
execFileSync("zip", ["-qr", outPptx, "."], { cwd: work });
console.log("Wrote", outPptx, fs.statSync(outPptx).size);
