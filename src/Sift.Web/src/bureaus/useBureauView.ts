import { useCallback, useEffect, useState } from 'react';

export type BureauView = { kind: 'list' } | { kind: 'detail'; id: string };

const listHash = '#/bureaus';
const detailPrefix = '#/bureaus/';

function safeDecode(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

function readView(): BureauView {
  const hash = window.location.hash;

  if (hash.startsWith(detailPrefix) && hash.length > detailPrefix.length) {
    return { kind: 'detail', id: safeDecode(hash.slice(detailPrefix.length)) };
  }

  return { kind: 'list' };
}

export function bureauHash(id: string): string {
  return `${detailPrefix}${encodeURIComponent(id)}`;
}

export function useBureauView() {
  const [view, setView] = useState<BureauView>(readView);

  useEffect(() => {
    const onHashChange = () => setView(readView());
    window.addEventListener('hashchange', onHashChange);

    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  const show = useCallback((next: BureauView) => {
    window.location.hash = next.kind === 'detail' ? bureauHash(next.id) : listHash;
  }, []);

  return { view, show };
}
