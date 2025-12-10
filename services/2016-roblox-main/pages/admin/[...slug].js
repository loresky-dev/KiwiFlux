import fs from 'fs';
import path from 'path';

export default function AdminPage() {
  // This never renders on the client; getServerSideProps serves the static HTML
  return null;
}

export async function getServerSideProps({ req, res }) {
  const adminIndexPath = path.join(process.cwd(), '..', 'admin', 'public', 'index.html');
  try {
    const contents = fs.readFileSync(adminIndexPath, 'utf8');
    res.setHeader('Content-Type', 'text/html');
    res.setHeader('Cache-Control', 'no-cache, no-store');
    res.statusCode = 200;
    res.end(contents);
    return { props: {} };
  } catch (e) {
    res.statusCode = 500;
    res.end('Admin UI not available');
    return { props: {} };
  }
}
