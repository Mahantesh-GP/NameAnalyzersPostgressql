(function(){
  const sources = new Map();

  function start(id, url, dotnetRef){
    stop(id);
    const es = new EventSource(url, { withCredentials: false });
    sources.set(id, es);

    es.addEventListener('open', () => {
      try { dotnetRef.invokeMethodAsync('OnSseOpen'); } catch {}
    });
    es.addEventListener('header', (e) => {
      try { dotnetRef.invokeMethodAsync('OnSseHeader', e.data); } catch {}
    });
    es.addEventListener('strong', (e) => {
      try { dotnetRef.invokeMethodAsync('OnSseStrong', e.data); } catch {}
    });
    es.addEventListener('similar', (e) => {
      try { dotnetRef.invokeMethodAsync('OnSseSimilar', e.data); } catch {}
    });
    es.addEventListener('complete', (e) => {
      try { dotnetRef.invokeMethodAsync('OnSseComplete', e.data); } catch {}
      stop(id);
    });
    es.addEventListener('error', (e) => {
      try { dotnetRef.invokeMethodAsync('OnSseError', 'error'); } catch {}
    });
  }

  function stop(id){
    const es = sources.get(id);
    if(es){
      try{ es.close(); }catch{}
      sources.delete(id);
    }
  }

  window.SSEClient = { start, stop };
})();