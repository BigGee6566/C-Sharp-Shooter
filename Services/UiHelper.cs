using System.Web;

namespace ForteLogisticsApp.Services
{
    public static class UiHelper
    {
        /// <summary>
        /// HTML-encodes a value for output in a data-binding expression.
        ///
        /// &lt;%# Eval("x") %&gt; writes the raw value, so any user-supplied text stored in the
        /// database (recipient names, addresses, parcel descriptions, notification text) would
        /// be rendered as live markup. Request validation blocks the crudest payloads on the
        /// way in, but it is an input filter, not an output guarantee - encoding at the point
        /// of output is what actually prevents stored XSS.
        /// </summary>
        public static string Enc(object value)
        {
            return value == null || value == System.DBNull.Value
                ? string.Empty
                : HttpUtility.HtmlEncode(value.ToString());
        }

        /// <summary>Encodes a value for safe use inside a JavaScript string literal.</summary>
        public static string JsEnc(object value)
        {
            if (value == null || value == System.DBNull.Value) return string.Empty;
            return HttpUtility.JavaScriptStringEncode(value.ToString());
        }

        /// <summary>
        /// Encodes a value for use inside a double-quoted CSS string (e.g. a print @page
        /// margin-box "content"). Anything other than letters, digits, space and a few safe
        /// punctuation marks becomes a CSS hex escape, so a quote, backslash, newline or
        /// "&lt;/style" in a user's name can never end the string or the style element.
        /// </summary>
        public static string CssStringEnc(object value)
        {
            if (value == null || value == System.DBNull.Value) return string.Empty;

            string s = value.ToString();
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool safe = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                            c == ' ' || c == '.' || c == ',' || c == ':' || c == '(' || c == ')' ||
                            c == '-' || c == '/' || c == '|';
                if (safe)
                {
                    sb.Append(c);
                    continue;
                }

                // A surrogate pair is ONE character (e.g. an emoji): escape its code point, not
                // the two halves, which CSS would render as replacement characters.
                int codePoint = c;
                if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(c, s[i + 1]);
                    i++;
                }
                sb.Append('\\').Append(codePoint.ToString("X6")).Append(' ');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a coloured status pill in the LogistiCore V5 style: a tinted background
        /// with same-hue dark text. Every pairing clears WCAG AA, which the solid-fill
        /// variants do not - white on amber or mid-grey lands around 2.2:1.
        /// </summary>
        public static string StatusBadge(string status)
        {
            string color;
            switch (status)
            {
                case "Delivered":
                case "Paid":
                case "Completed":
                    color = "bg-emerald-100 text-emerald-800"; break;
                case "Failed":
                case "Lost":
                case "Damaged":
                case "Unpaid":
                    color = "bg-rose-100 text-rose-800"; break;
                case "InTransit":
                case "OutForDelivery":
                case "Active":
                    color = "bg-blue-100 text-blue-800"; break;
                case "Sorted":
                case "Received":
                    color = "bg-purple-100 text-purple-800"; break;
                case "Pending":
                case "Registered":
                    color = "bg-amber-100 text-amber-800"; break;
                default:
                    color = "bg-gray-100 text-gray-800"; break;
            }
            return "<span class='inline-flex items-center font-label-sm text-label-sm font-semibold " +
                   "px-3 py-1 rounded-full uppercase tracking-wide " + color + "'>" +
                   HttpUtility.HtmlEncode(status ?? "") + "</span>";
        }

        /// <summary>
        /// CSS classes for an inline alert / status message (spec 9). Pairs with the
        /// .fcl-alert-* rules in Site1.Master, so alert styling lives in one place rather
        /// than being spelled out at each of the ~39 call sites.
        ///
        /// kind: "success" | "error" | "warning" | "info" (anything else falls back to info).
        /// </summary>
        public static string AlertClass(string kind)
        {
            switch ((kind ?? string.Empty).ToLowerInvariant())
            {
                case "success": return "fcl-alert fcl-alert-success";
                case "error":   return "fcl-alert fcl-alert-error";
                case "warning": return "fcl-alert fcl-alert-warning";
                default:        return "fcl-alert fcl-alert-info";
            }
        }

        /// <summary>Convenience overload: success when ok, error otherwise.</summary>
        public static string AlertClass(bool ok)
        {
            return AlertClass(ok ? "success" : "error");
        }
    }
}
