import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const configPath = fileURLToPath(new URL('../public/runtime-config.js', import.meta.url));
const mode = process.argv[2];

function validate(value) {
  let url;
  try { url = new URL(value); }
  catch { throw new Error('Địa chỉ API không hợp lệ.'); }
  if (url.protocol !== 'https:' || url.username || url.password ||
      url.pathname !== '/' || url.search || url.hash)
    throw new Error('Địa chỉ API phải là origin HTTPS, không chứa đường dẫn hay thông tin đăng nhập.');
  return url.origin;
}

try {
  if (mode === 'set') {
    const origin = validate(process.argv[3] || '');
    writeFileSync(configPath, `window.JOURNEY_API_URL = ${JSON.stringify(origin)};\n`);
    console.log('Đã lưu địa chỉ API công khai cho bản web và di động.');
  } else if (mode === 'check') {
    const text = readFileSync(configPath, 'utf8');
    const match = text.match(/window\.JOURNEY_API_URL\s*=\s*(['"])(.*?)\1\s*;/);
    if (!match || !match[2]) throw new Error('Chưa cấu hình API. Hãy đặt địa chỉ HTTPS bằng api-config.mjs set.');
    validate(match[2]);
    console.log('Địa chỉ API di động hợp lệ.');
  } else {
    throw new Error('Cách dùng: api-config.mjs set <HTTPS API origin> hoặc api-config.mjs check');
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
