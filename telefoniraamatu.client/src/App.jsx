import { useMemo, useState, useEffect } from 'react'
import './App.css'

/**
 * Формирует HTTP-заголовки для запросов к API.
 * Если есть `token`, добавляет `Authorization: Bearer <token>`.
 * Иначе, если есть `anonId`, добавляет заголовок `X-Anonymous-Id`.
 * Возвращает мемоизированный объект заголовков при изменении `token`/`anonId`.
 */
function useApiHeaders(token, anonId) {
  return useMemo(() => {
    const headers = { 'Content-Type': 'application/json' }
    if (token) headers['Authorization'] = `Bearer ${token}`
    else if (anonId) headers['X-Anonymous-Id'] = anonId
    return headers
  }, [token, anonId])
}

/**
 * Универсальная обёртка над `fetch` для запросов к бекенду с префиксом `/api`.
 * - Поддерживает методы и произвольное тело (JSON-сериализация).
 * - Для DELETE включает `keepalive`, чтобы запрос завершался при закрытии страницы.
 * - Бросает ошибку для не-OK ответов, возвращает `null` для 204.
 * - Мягко игнорирует прерванные DELETE-запросы.
 */
async function apiFetch(path, method, headers, body) {
  try {
    const res = await fetch(`/api${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
      keepalive: method === 'DELETE'
    })
    if (!res.ok) throw new Error(await res.text())
    return res.status === 204 ? null : await res.json()
  } catch (e) {
    const msg = String(e?.message || e)
    if (method === 'DELETE' && (msg.includes('ERR_ABORTED') || e?.name === 'AbortError')) {
      return null
    }
    throw e
  }
}

/**
 * Главный компонент приложения «Telefoniraamat».
 * Управляет аутентификацией, списком контактов и групп,
 * а также операциями добавления/удаления и связями контакт↔группа.
 */
function App() {
  const [token, setToken] = useState(localStorage.getItem('token') || '')
  const [anonId] = useState(localStorage.getItem('anonId') || '')
  const headers = useApiHeaders(token, anonId)

  const [auth, setAuth] = useState({ username: '', password: '' })
  const [contacts, setContacts] = useState([])
  const [groups, setGroups] = useState([])
  const [search, setSearch] = useState('')
  const [groupSearch, setGroupSearch] = useState('')
  const [newContact, setNewContact] = useState({ firstName: '', lastName: '', phone: '', markmed: '' })
  const [newGroupName, setNewGroupName] = useState('')
  const [confirmState, setConfirmState] = useState({ open: false, message: '', resolve: null })

  /**
   * Форматирует контакт в компактную строку: имя, фамилия, доп.поле, телефон.
   * Берёт первый телефон и первое пользовательское поле, если они есть.
   */
  function formatContact(c) {
    const parts = (c.name || '').split(' ')
    const first = parts[0] || ''
    const last = parts.slice(1).join(' ')
    const phone = c.phoneNumbers?.[0] || ''
    const cf = Array.isArray(c.customFields) && c.customFields.length ? c.customFields[0] : null
    const custom = cf ? cf.value : ''
    return [first, last, custom, phone].filter(x => x).join('-')
  }

  

  /**
   * Регистрирует пользователя и сохраняет токен.
   * После регистрации загружает контакты и группы с авторизационными заголовками.
   */
  async function register() {
    const data = await apiFetch('/auth/register', 'POST', headers, auth)
    localStorage.setItem('token', data.token)
    setToken(data.token)
    const authHeaders = { 'Content-Type': 'application/json', Authorization: `Bearer ${data.token}` }
    await loadContacts(authHeaders)
    await loadGroups(authHeaders)
  }

  /**
   * Авторизует пользователя и сохраняет токен.
   * После входа загружает контакты и группы с авторизационными заголовками.
   */
  async function login() {
    const data = await apiFetch('/auth/login', 'POST', headers, auth)
    localStorage.setItem('token', data.token)
    setToken(data.token)
    const authHeaders = { 'Content-Type': 'application/json', Authorization: `Bearer ${data.token}` }
    await loadContacts(authHeaders)
    await loadGroups(authHeaders)
  }

  

  /**
   * Загружает список контактов.
   * Использует переопределённые заголовки, либо строит их из текущего `token/anonId`.
   * Поддерживает серверный поиск по строке `search`.
   */
  async function loadContacts(headersOverride) {
    try {
      const h = headersOverride ?? (() => {
        const h0 = { 'Content-Type': 'application/json' }
        if (token) h0['Authorization'] = `Bearer ${token}`
        else if (anonId) h0['X-Anonymous-Id'] = anonId
        return h0
      })()
      const data = await apiFetch(`/contacts${search ? `?search=${encodeURIComponent(search)}` : ''}`, 'GET', h)
      setContacts(data)
    } catch (e) {
      console.error('Kontaktide laadimine ebaõnnestus', e)
    }
  }

  /**
   * Загружает список групп.
   * Использует переопределённые заголовки, либо строит их из текущего `token/anonId`.
   */
  async function loadGroups(headersOverride) {
    try {
      const h = headersOverride ?? (() => {
        const h0 = { 'Content-Type': 'application/json' }
        if (token) h0['Authorization'] = `Bearer ${token}`
        else if (anonId) h0['X-Anonymous-Id'] = anonId
        return h0
      })()
      const data = await apiFetch(`/groups${groupSearch ? `?search=${encodeURIComponent(groupSearch)}` : ''}`, 'GET', h)
      setGroups(data)
    } catch (e) {
      console.error('Gruppide laadimine ebaõnnestus', e)
    }
  }

  /**
   * Добавляет новый контакт по упрощённой форме.
   * Валидирует наличие имени/фамилии и телефона, затем отправляет запрос.
   * После успешного добавления очищает форму и перезагружает список.
   */
  async function addContact() {
    const first = (newContact.firstName || '').trim()
    const last = (newContact.lastName || '').trim()
    const phone = (newContact.phone || '').trim()
    const notes = (newContact.markmed || '').trim()
    if (!( (first || last) && phone )) return
    const body = { nimi: first, perekonnanimi: last, markmed: notes, telefon: phone }
    await apiFetch('/contacts/simple', 'POST', headers, body)
    setNewContact({ firstName: '', lastName: '', phone: '', markmed: '' })
    await loadContacts()
  }

  /**
   * Удаляет контакт по `id` после подтверждения пользователем.
   * При успехе обновляет списки контактов и групп.
   */
  async function deleteContact(id) {
    const cx = contacts.find(c => c.id === id)
    const name = cx?.name || id
    const ok = await new Promise(res => setConfirmState({ open: true, message: `Kas olete kindel, et soovite kustutada kontakti "${name}"?`, resolve: res }))
    if (!ok) return
    try {
      await apiFetch(`/contacts/${id}`, 'DELETE', headers)
      await loadContacts()
      await loadGroups()
    } catch (e) {
      console.error('Kontakt ei kustunud', e)
      alert('Kontakt ei kustunud: ' + (e?.message || e))
    }
  }

  /**
   * Создаёт новую группу по введённому имени.
   * После создания очищает поле и перезагружает список групп.
   */
  async function createGroup() {
    const name = (newGroupName || '').trim()
    if (!name) return
    await apiFetch('/groups', 'POST', headers, { name })
    setNewGroupName('')
    await loadGroups()
  }

  /**
   * Удаляет группу по `id` после подтверждения пользователем.
   * При успехе обновляет список групп.
   */
  async function deleteGroup(id) {
    const gx = groups.find(g => g.id === id)
    const name = gx?.name || id
    const ok = await new Promise(res => setConfirmState({ open: true, message: `Kas olete kindel, et soovite kustutada grupi "${name}"?`, resolve: res }))
    if (!ok) return
    try {
      await apiFetch(`/groups/${id}`, 'DELETE', headers)
      await loadGroups()
    } catch (e) {
      console.error('Gruppi ei kustunud', e)
      alert('Gruppi ei kustunud: ' + (e?.message || e))
    }
  }

  /**
   * Добавляет контакт `contactId` в группу `groupId` и затем обновляет группы.
   */
  async function addContactToGroup(groupId, contactId) {
    await apiFetch(`/groups/${groupId}/contacts`, 'POST', headers, { contactId })
    await loadGroups()
  }

  /**
   * Удаляет контакт `contactId` из группы `groupId` после подтверждения.
   * При ошибке показывает сообщение и логирует проблему.
   */
  async function removeContactFromGroup(groupId, contactId) {
    const gx = groups.find(g => g.id === groupId)
    const cx = contacts.find(c => c.id === contactId)
    const gname = gx?.name || groupId
    const cname = cx?.name || contactId
    const ok = await new Promise(res => setConfirmState({ open: true, message: `Kas olete kindel, et soovite eemaldada kontakti "${cname}" grupist "${gname}"?`, resolve: res }))
    if (!ok) return
    try {
      await apiFetch(`/groups/${groupId}/contacts/${contactId}`, 'DELETE', headers)
      await loadGroups()
    } catch (e) {
      console.error('Kontakti eemaldamine grupist ebaõnnestus', e)
      alert('Kontakti eemaldamine grupist ebaõnnestus: ' + (e?.message || e))
    }
  }


  

  /**
   * Дебаунс-загрузка контактов при изменении строки поиска или статуса авторизации.
   * Ждёт 300 мс и запрашивает контакты, если есть `token` или `anonId`.
   */
  useEffect(() => {
    const t = setTimeout(() => {
      if (token || anonId) loadContacts()
    }, 300)
    return () => clearTimeout(t)
  }, [search, token, anonId])

  useEffect(() => {
    const t = setTimeout(() => {
      if (token || anonId) loadGroups()
    }, 300)
    return () => clearTimeout(t)
  }, [groupSearch, token, anonId])

  /**
   * Выход из аккаунта: удаляет токен и очищает локальное состояние.
   */
  function logout() {
    localStorage.removeItem('token')
    setToken('')
    setContacts([])
    setGroups([])
  }

  return (
    <div className="app">
      <h1 className="title">Telefoniraamat</h1>

      <div className="toolbar" style={{ justifyContent: 'center' }}>
        <input className="input" placeholder="Kasutajanimi" value={auth.username} onChange={e => setAuth(a => ({ ...a, username: e.target.value }))} />
        <input className="input" type="password" placeholder="Parool" value={auth.password} onChange={e => setAuth(a => ({ ...a, password: e.target.value }))} />
        <button className="btn btn-accent" type="button" onClick={register}>Registreeru</button>
        <button className="btn btn-accent" type="button" onClick={login}>Logi sisse</button>
        <button className="btn btn-accent" type="button" onClick={logout} disabled={!token}>Logi välja</button>
      </div>

      {token && (
        <>
          <div className="section-bar">Kontaktid</div>
          <div className="section">
            <div className="row">
              <input className="input" placeholder="Otsing" value={search} onChange={e => setSearch(e.target.value)} />
            </div>

            <div className="row" style={{ marginTop: 8 }}>
              <input className="input" placeholder="Nimi" value={newContact.firstName} onChange={e => setNewContact(c => ({ ...c, firstName: e.target.value }))} />
              <input className="input" placeholder="Perekonnanimi" value={newContact.lastName} onChange={e => setNewContact(c => ({ ...c, lastName: e.target.value }))} />
              <input className="input" placeholder="Märkmed" value={newContact.markmed} onChange={e => setNewContact(c => ({ ...c, markmed: e.target.value }))} />
              <input
                className="input"
                placeholder="Telefon"
                inputMode="numeric"
                pattern="[0-9]*"
                value={newContact.phone}
                onChange={e => {
                  const v = (e.target.value || '').replace(/[^0-9]/g, '')
                  setNewContact(c => ({ ...c, phone: v }))
                }}
              />
              <button className="btn btn-dark" type="button" onClick={addContact} disabled={!((newContact.firstName.trim() || newContact.lastName.trim()) && newContact.phone.trim())}>Lisa kontakt</button>
            </div>

            <ul className="list">
              {contacts.map(c => (
                <li key={c.id} className="list-item">
                  <span>{c.name}</span>
                  {Array.isArray(c.customFields) && c.customFields[0]?.value && <span> — {c.customFields[0].value}</span>}
                  {c.phoneNumbers?.[0] && <span> ({c.phoneNumbers[0]})</span>}
                  <button className="btn btn-dark" type="button" onClick={() => deleteContact(c.id)}>Kustuta</button>
                </li>
              ))}
            </ul>
          </div>

          <div className="section-line"></div>
          <h2 className="subtitle">Grupid</h2>
            <div className="row">
              <input className="input" placeholder="Otsing" value={groupSearch} onChange={e => setGroupSearch(e.target.value)} />
            </div>
            <div className="row" style={{ marginTop: 8 }}>
              <input className="input" placeholder="Grupi nimi" value={newGroupName} onChange={e => setNewGroupName(e.target.value)} />
              <button className="btn btn-dark" type="button" onClick={createGroup} disabled={!newGroupName.trim()}>Lisa grupp</button>
            </div>
            <div className="groups-scroll">
            <ul>
              {groups.map(g => (
                <li key={g.id}>
                  <div className="row">
                    <strong>{g.name}</strong>
                    <button className="btn btn-dark" type="button" onClick={() => deleteGroup(g.id)}>Kustuta grupp</button>
                  </div>
                  <div>
                    <ul>
                      {g.contactIds?.map(cid => {
                        const cx = contacts.find(c => c.id === cid)
                        return (
                          <li key={cid} className="list-item">
                            <span>{cx ? formatContact(cx) : cid}</span>
                            <button className="btn btn-dark" type="button" onClick={() => removeContactFromGroup(g.id, cid)}>Eemalda</button>
                          </li>
                        )
                      })}
                    </ul>
                    <div className="row">
                      <select className="select" onChange={e => addContactToGroup(g.id, e.target.value)}>
                        <option value="">Lisa kontakt gruppi</option>
                        {contacts.map(c => (
                          <option key={c.id} value={c.id}>{c.name}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
            </div>
          
        </>
      )}
      {confirmState.open && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ background: '#fff', color: '#000', padding: 16, borderRadius: 8, minWidth: 300 }}>
            <div style={{ marginBottom: 12 }}>{confirmState.message}</div>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" onClick={() => { confirmState.resolve?.(false); setConfirmState({ open: false, message: '', resolve: null }) }}>Ei</button>
              <button type="button" onClick={() => { confirmState.resolve?.(true); setConfirmState({ open: false, message: '', resolve: null }) }}>Jah</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default App
