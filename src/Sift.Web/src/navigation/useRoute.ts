import { useCallback, useEffect, useState } from 'react';
import { parseRoute, type AppRoute } from './routes';

export function useRoute() {
  const [route, setRoute] = useState<AppRoute>(() => parseRoute(window.location.hash));

  useEffect(() => {
    const onHashChange = () => setRoute(parseRoute(window.location.hash));
    window.addEventListener('hashchange', onHashChange);

    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  const navigate = useCallback((hash: string) => {
    if (window.location.hash === hash) {
      setRoute(parseRoute(hash));
    } else {
      window.location.hash = hash;
    }
  }, []);

  return { route, navigate };
}
