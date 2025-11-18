const fs = require('fs');
const path = require('path');

// Try to load archiver from global npm or local node_modules
let archiver;
try {
    archiver = require('archiver');
} catch (e) {
    try {
        // Try global npm location
        const globalPath = 'C:\\Users\\Plex\\AppData\\Roaming\\npm\\node_modules\\archiver';
        archiver = require(globalPath);
    } catch (e2) {
        console.error('Error: archiver module not found. Install with: npm install -g archiver');
        process.exit(1);
    }
}

const sourceDir = process.argv[2];
const outputZip = process.argv[3];

if (!sourceDir || !outputZip) {
    console.error('Usage: node create-zip.js <source_dir> <output_zip>');
    process.exit(1);
}

if (!fs.statSync(sourceDir).isDirectory()) {
    console.error('Error: Source directory not found:', sourceDir);
    process.exit(1);
}

const output = fs.createWriteStream(outputZip);
const archive = archiver('zip', { zlib: { level: 9 } });

output.on('close', () => {
    const sizeBytes = fs.statSync(outputZip).size;
    const sizeMB = (sizeBytes / 1024 / 1024).toFixed(2);
    console.log(`ZIP created: ${outputZip} (${sizeMB} MB)`);
});

archive.on('error', (err) => {
    console.error('Archive error:', err);
    process.exit(1);
});

archive.pipe(output);

// Add all contents from source directory
const items = fs.readdirSync(sourceDir);
items.forEach(item => {
    const itemPath = path.join(sourceDir, item);
    const stat = fs.statSync(itemPath);
    if (stat.isDirectory()) {
        archive.directory(itemPath, path.basename(itemPath));
    } else {
        archive.file(itemPath, { name: path.basename(itemPath) });
    }
});

archive.finalize();
