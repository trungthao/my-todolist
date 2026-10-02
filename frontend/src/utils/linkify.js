// Order matters: URLs first so emails/phone numbers inside a URL are not split out.
const PATTERN = new RegExp(
  [
    '(?<url>\\b(?:https?:\\/\\/|www\\.)[^\\s<>"\']+)',
    '(?<email>\\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}\\b)',
    '(?<phone>(?<![\\w+])(?:\\+\\d{1,3}[ .-]?|0)\\d(?:[ .-]?\\d){7,11}(?![\\w]))'
  ].join('|'),
  'g'
)

const TRAILING_PUNCTUATION = /[.,;:!?'")\]}]+$/

function trimUrl(raw) {
  let url = raw.replace(TRAILING_PUNCTUATION, '')
  // Keep a closing paren that balances one inside the URL, e.g. wiki links.
  while (raw.length > url.length && raw[url.length] === ')' &&
    (url.match(/\(/g) || []).length > (url.match(/\)/g) || []).length) {
    url += ')'
  }
  return url
}

/**
 * Split text into segments: { type: 'text' | 'url' | 'email' | 'phone', text, href? }.
 */
export function linkify(text) {
  if (!text) return []
  const segments = []
  let last = 0

  for (const match of text.matchAll(PATTERN)) {
    const { url, email, phone } = match.groups
    let value = match[0]
    let segment

    if (url) {
      value = trimUrl(url)
      segment = { type: 'url', text: value, href: /^https?:\/\//i.test(value) ? value : `https://${value}` }
    } else if (email) {
      segment = { type: 'email', text: value, href: `mailto:${value}` }
    } else if (phone) {
      const digits = value.replace(/[^\d+]/g, '')
      if (digits.replace('+', '').length < 9) continue
      segment = { type: 'phone', text: value, href: `tel:${digits}` }
    }

    if (match.index > last) segments.push({ type: 'text', text: text.slice(last, match.index) })
    segments.push(segment)
    last = match.index + value.length
  }

  if (last < text.length) segments.push({ type: 'text', text: text.slice(last) })
  return segments
}
