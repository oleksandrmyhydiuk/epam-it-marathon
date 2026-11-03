const DEMO_JWT_HEADER = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
const DEMO_JWT_PAYLOAD = btoa(
  JSON.stringify({
    sub: 'demo-owner-id',
    email: 'demo@epam.com',
    role: 'Customer',
    firstName: 'Yevhenii',
    lastName: 'Owner',
    exp: 4102444800,
  })
);
export const DEMO_ID_TOKEN = `${DEMO_JWT_HEADER}.${DEMO_JWT_PAYLOAD}.demo-signature`;

export function initDemoAuth(
  search: string = typeof window !== 'undefined' ? window.location.search : ''
): void {
  if (typeof window === 'undefined') {
    return;
  }

  const params = new URLSearchParams(search);
  if (!params.has('demo')) {
    return;
  }

  localStorage.setItem(
    'auth',
    JSON.stringify({
      idToken: DEMO_ID_TOKEN,
      accessToken: DEMO_ID_TOKEN,
    })
  );

  localStorage.setItem(
    'user',
    JSON.stringify({
      firstName: 'Yevhenii',
      lastName: 'Owner',
      email: 'demo@epam.com',
      role: 'Customer',
    })
  );
}
