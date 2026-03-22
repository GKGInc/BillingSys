// Google Identity Services (GIS) — ID token in memory + sessionStorage for tab refresh persistence.
window.billingSysAuth = (function () {
  const STORAGE_KEY = 'billingSysGisIdToken';
  let idToken = null;
  let dotNetRef = null;

  function decodePayload(jwt) {
    try {
      const parts = jwt.split('.');
      if (parts.length !== 3) return null;
      let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (base64.length % 4) base64 += '=';
      const json = atob(base64);
      return JSON.parse(json);
    } catch {
      return null;
    }
  }

  function isExpired(jwt) {
    const p = decodePayload(jwt);
    if (!p || typeof p.exp !== 'number') return true;
    const expMs = p.exp * 1000;
    return Date.now() >= expMs - 60000;
  }

  function persistToken(jwt) {
    try {
      if (jwt) {
        sessionStorage.setItem(STORAGE_KEY, jwt);
      } else {
        sessionStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      /* private mode / blocked storage */
    }
  }

  function loadTokenFromStorage() {
    if (idToken) return;
    try {
      const s = sessionStorage.getItem(STORAGE_KEY);
      if (s && !isExpired(s)) {
        idToken = s;
      } else if (s) {
        sessionStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      /* ignore */
    }
  }

  function removeSignInModal() {
    const modal = document.getElementById('gis-signin-modal');
    if (modal) modal.remove();
  }

  function showSignInModal() {
    // Don't create duplicate modals
    if (document.getElementById('gis-signin-modal')) return;

    // Create overlay
    const overlay = document.createElement('div');
    overlay.id = 'gis-signin-modal';
    overlay.style.cssText = [
      'position:fixed',
      'top:0',
      'left:0',
      'width:100%',
      'height:100%',
      'background:rgba(0,0,0,0.5)',
      'display:flex',
      'align-items:center',
      'justify-content:center',
      'z-index:9999'
    ].join(';');

    // Create card
    const card = document.createElement('div');
    card.style.cssText = [
      'background:#fff',
      'border-radius:8px',
      'padding:32px',
      'display:flex',
      'flex-direction:column',
      'align-items:center',
      'gap:16px',
      'box-shadow:0 4px 24px rgba(0,0,0,0.2)'
    ].join(';');

    // Title
    const title = document.createElement('p');
    title.textContent = 'Sign in with your Tech85 Google account';
    title.style.cssText = 'margin:0;font-size:16px;font-weight:500;color:#333;font-family:sans-serif';
    card.appendChild(title);

    // Container for the Google button
    const buttonContainer = document.createElement('div');
    buttonContainer.id = 'gis-signin-button';
    card.appendChild(buttonContainer);

    // Cancel button
    const cancel = document.createElement('button');
    cancel.textContent = 'Cancel';
    cancel.style.cssText = [
      'margin-top:8px',
      'padding:8px 24px',
      'border:1px solid #ccc',
      'border-radius:4px',
      'background:#fff',
      'cursor:pointer',
      'font-size:14px',
      'color:#666'
    ].join(';');
    cancel.onclick = function () {
      removeSignInModal();
    };
    card.appendChild(cancel);

    overlay.appendChild(card);
    document.body.appendChild(overlay);

    // Render the Google sign-in button inside the modal
    google.accounts.id.renderButton(
      document.getElementById('gis-signin-button'),
      {
        type: 'standard',
        shape: 'rectangular',
        theme: 'outline',
        text: 'signin_with',
        size: 'large',
        logo_alignment: 'left'
      }
    );
  }

  function handleCredentialResponse(response) {
    if (response && response.credential) {
      idToken = response.credential;
      persistToken(idToken);
      removeSignInModal();
      if (dotNetRef) {
        dotNetRef.invokeMethodAsync('NotifyTokenChanged');
      }
    }
  }

  function waitForGis(callback, attempt) {
    if (window.google && window.google.accounts && window.google.accounts.id) {
      callback();
      return;
    }
    if (attempt > 200) {
      console.error('[billingSysAuth] Google GIS client failed to load.');
      return;
    }
    setTimeout(function () { waitForGis(callback, attempt + 1); }, 50);
  }

  return {
    initializeGoogleAuth: function (clientId, dotnetHelper) {
      dotNetRef = dotnetHelper;
      waitForGis(function () {
        google.accounts.id.initialize({
          client_id: clientId,
          callback: handleCredentialResponse,
          auto_select: false,
          cancel_on_tap_outside: true,
          // Avoid FedCM AbortError / flaky One Tap in Chrome when signal aborts early
          use_fedcm_for_prompt: false
        });
        loadTokenFromStorage();
      }, 0);
    },

    promptSignIn: function () {
      waitForGis(function () {
        // First try One Tap
        google.accounts.id.prompt(function (notification) {
          const notDisplayed = notification.isNotDisplayed && notification.isNotDisplayed();
          const skipped = notification.isSkippedMoment && notification.isSkippedMoment();
          const dismissed = notification.isDismissedMoment && notification.isDismissedMoment();

          if (notDisplayed || skipped || dismissed) {
            // One Tap failed or was dismissed — show a modal with a rendered sign-in button
            showSignInModal();
          }
        });
      }, 0);
    },

    getStoredToken: function () {
      loadTokenFromStorage();
      if (!idToken) return null;
      if (isExpired(idToken)) {
        idToken = null;
        persistToken(null);
        if (dotNetRef) {
          dotNetRef.invokeMethodAsync('NotifyTokenExpired');
        }
        return null;
      }
      return idToken;
    },

    clearToken: function () {
      idToken = null;
      persistToken(null);
      removeSignInModal();
      waitForGis(function () {
        google.accounts.id.disableAutoSelect();
      }, 0);
    }
  };
})();
