import https from 'node:https';

const targets = [
  ['https://dv-g3b-emudhra.we-have-a-problem.com/', false],
  ['https://dv-g3b-aaa-emudhra.we-have-a-problem.com/', false],
  ['https://example.com/', true],
];
const results = [];
for (const [url, trusted] of targets) {
  for (let attempt = 1; attempt <= 3; attempt++) {
    try {
      const status = await new Promise((resolve, reject) => {
        const request = https.get(url, { family: 4, agent: false }, response => {
          response.resume();
          resolve(response.statusCode);
        });
        const deadline = setTimeout(() => request.destroy(new Error('timeout')), 20000);
        request.on('close', () => clearTimeout(deadline));
        request.on('error', reject);
      });
      results.push({ url, trusted, attempt, status, outcome: trusted && status === 200 ? 'pass' : 'fail' });
      break;
    } catch (error) {
      const code = error.code ?? error.message;
      const rejected = /^(CERT_REVOKED|CERT_UNTRUSTED|CERT_SIGNATURE_FAILURE|UNABLE_TO_GET_ISSUER_CERT_LOCALLY|UNABLE_TO_VERIFY_LEAF_SIGNATURE)$/.test(String(code));
      const outcome = !trusted && rejected ? 'pass' : 'inconclusive';
      if (outcome === 'inconclusive' && attempt < 3) {
        console.error(JSON.stringify({ url, attempt, code, outcome }));
        continue;
      }
      results.push({ url, trusted, attempt, code, outcome });
      break;
    }
  }
}
console.log(JSON.stringify({ runtime: process.version, checkedAt: new Date().toISOString(), results }, null, 2));
if (results.some(result => result.outcome !== 'pass')) process.exitCode = 1;
