import { useEffect, useState } from "react"
import AuthenticationStore from "../stores/authentication"

const IndexPage = props => {
  const auth = AuthenticationStore.useContainer();
  const [autoLoginAttempted, setAutoLoginAttempted] = useState(false);

  useEffect(() => {
    // Auto-login if not authenticated
    if (!autoLoginAttempted && !auth.isAuthenticated && !auth.isPending) {
      setAutoLoginAttempted(true);
      // Check if we have the .ROBLOSECURITY cookie
      const hasCookie = document.cookie.includes('.ROBLOSECURITY');
      if (!hasCookie) {
        // Auto-login as roblox user
        fetch('/api/mock-auth?action=login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ username: 'Roblox', password: 'Roblox' }),
          credentials: 'include'
        }).then(() => {
          // Reload after successful login
          setTimeout(() => window.location.reload(), 500);
        }).catch(e => {
          console.error('Auto-login failed:', e);
        });
      }
    }

    // Redirect to home if authenticated
    if (auth.isAuthenticated && !auth.isPending) {
      window.location.href = '/home';
    }
  }, [auth.isAuthenticated, auth.isPending, autoLoginAttempted]);

  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', fontFamily: 'Arial' }}>
      <div style={{ textAlign: 'center' }}>
        <h2>Loading...</h2>
        <p>Setting up your session...</p>
      </div>
    </div>
  )
}

export default IndexPage;