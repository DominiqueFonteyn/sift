import createClient, { type Middleware } from 'openapi-fetch';
import { expireSession, getSessionState } from '../identity/sessionStore';
import type { paths } from './generated/schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5100';

const bearerAuth: Middleware = {
  onRequest({ request }) {
    const { session } = getSessionState();

    if (session) {
      request.headers.set('Authorization', `Bearer ${session.accessToken}`);
    }

    return request;
  },
  onResponse({ request, response }) {
    // A 401 from login itself means wrong credentials, not an ended session.
    const isLogin = new URL(request.url).pathname.endsWith('/auth/login');

    if (response.status === 401 && !isLogin && getSessionState().session) {
      expireSession();
    }

    return response;
  },
};

export const apiClient = createClient<paths>({ baseUrl });

apiClient.use(bearerAuth);
