using System.Security.Cryptography;
using System.Text;
using Contracts.Application.Validation;

namespace UI.Application.Features.Avatars;

public sealed record GetAvatarQuery(string Login) : IQuery<string>;

internal sealed class GetAvatarQueryHandler : IQueryHandler<GetAvatarQuery, string>
{
    public Task<Result<string>> Handle(GetAvatarQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var login = request.Login?.Trim().ToLowerInvariant() ?? "";
        if (!ProfileLogin.IsValid(login))
            return Task.FromResult(Result<string>.Error(UiErrorCodes.InvalidLogin));

        // Profile Collection's NFT index is the unsigned big-endian SHA-256 of login UTF-8.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(login));
        var hue = ((hash[0] << 8) | hash[1]) % 360;
        var accent = $"hsl({hue}, 72%, 62%)";
        var highlight = $"hsl({(hue + 36) % 360}, 85%, 88%)";
        var shadow = $"hsl({hue}, 62%, 24%)";
        var orbitRotation = hash[3] % 90;
        // Reserve 48px on either side; explicit textLength keeps font fallbacks inside this width.
        const int loginMaxWidth = 416;
        var fontSize = Math.Min(64, loginMaxWidth * 5 / (3 * login.Length));
        var loginTextWidth = fontSize * 3 * login.Length / 5;

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512" role="img" aria-label="CryptoStyle avatar">
              <defs>
                <linearGradient id="metal" x1="12%" y1="100%" x2="80%" y2="0%">
                  <stop stop-color="{shadow}"/><stop offset=".25" stop-color="{accent}"/>
                  <stop offset=".49" stop-color="{highlight}"/><stop offset=".52" stop-color="{accent}"/>
                  <stop offset=".8" stop-color="{highlight}"/><stop offset="1" stop-color="#ffffff"/>
                </linearGradient>
                <radialGradient id="ambient" cx="28%" cy="15%" r="90%">
                  <stop stop-color="{accent}" stop-opacity=".32"/><stop offset=".65" stop-color="{shadow}" stop-opacity=".15"/><stop offset="1" stop-color="#070a13"/>
                </radialGradient>
                <linearGradient id="ribbon" x2="100%">
                  <stop stop-color="#0a0e19"/><stop offset=".5" stop-color="{shadow}"/><stop offset="1" stop-color="#0a0e19"/>
                </linearGradient>
                <linearGradient id="rim" x2="100%" y2="100%">
                  <stop stop-color="{highlight}" stop-opacity=".7"/><stop offset=".45" stop-color="{accent}" stop-opacity=".08"/><stop offset="1" stop-color="{accent}" stop-opacity=".45"/>
                </linearGradient>
                <filter id="relief" x="-15%" y="-15%" width="140%" height="145%">
                  <feDropShadow dx="0" dy="5" stdDeviation="0" flood-color="{shadow}"/>
                  <feDropShadow dx="0" dy="12" stdDeviation="10" flood-color="#000000" flood-opacity=".65"/>
                </filter>
                <clipPath id="frame"><rect width="512" height="512" rx="64"/></clipPath>
              </defs>
              <g clip-path="url(#frame)">
                <rect width="512" height="512" fill="#090d17"/>
                <rect width="512" height="512" fill="url(#ambient)"/>
                <g transform="rotate({orbitRotation} 256 226)" fill="none" stroke="{accent}">
                  <ellipse cx="256" cy="226" rx="298" ry="175" stroke-opacity=".12"/>
                  <ellipse cx="256" cy="226" rx="310" ry="187" stroke-opacity=".07"/>
                  <ellipse cx="256" cy="226" rx="322" ry="199" stroke-opacity=".05"/>
                </g>
                <path d="M-40 408L390 -22M60 534L552 42" stroke="{highlight}" stroke-opacity=".035" stroke-width="44"/>
                <rect x="15" y="15" width="482" height="482" rx="51" fill="none" stroke="url(#rim)"/>
                <rect x="24" y="24" width="464" height="464" rx="44" fill="none" stroke="{accent}" stroke-opacity=".07"/>
                <path d="M238 57L256 51L274 57L256 63Z" fill="{accent}"/>
                <circle cx="220" cy="57" r="2" fill="{highlight}" opacity=".6"/>
                <circle cx="292" cy="57" r="2" fill="{highlight}" opacity=".6"/>
              </g>
              <g fill="url(#metal)" stroke="{highlight}" stroke-width=".7" filter="url(#relief)">
                <path d="M230 92V172H216C203 123 181 110 153 110C108 110 83 149 83 213C83 277 108 315 153 315C185 315 206 296 220 257H233L227 330H216L209 312C192 328 170 336 144 336C75 336 42 283 42 215C42 143 84 90 147 90C177 90 194 99 210 111L218 92Z"/>
                <path d="M445 93V164H432C422 125 399 109 373 109C341 109 321 125 321 148C321 174 344 184 380 198C427 216 458 233 458 275C458 316 425 338 379 338C350 338 325 328 308 316L301 334H289V258H303C315 300 342 319 374 319C406 319 427 304 427 280C427 255 402 244 366 230C319 212 289 194 289 153C289 117 320 90 365 90C391 90 414 99 430 110L436 93Z"/>
              </g>
              <path d="M32 187H480L468 219L480 251H32L44 219Z" fill="url(#ribbon)"/>
              <path d="M56 188H456M56 250H456" stroke="url(#rim)" stroke-width="1"/>
              <text x="256" y="230" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="30" font-weight="700" letter-spacing="3" textLength="372" lengthAdjust="spacingAndGlyphs" fill="{highlight}">CRYPTO STYLE</text>
              <path d="M218 375H244M268 375H294" stroke="{accent}" stroke-opacity=".45"/>
              <path d="M252 375L256 371L260 375L256 379Z" fill="{accent}"/>
              <text id="login" x="256" y="440" text-anchor="middle" font-family="Georgia, 'Times New Roman', serif" font-size="{fontSize}" font-weight="600" textLength="{loginTextWidth}" lengthAdjust="spacingAndGlyphs" fill="url(#metal)" stroke="{highlight}" stroke-width=".7" filter="url(#relief)">{login}</text>
            </svg>
            """;
        return Task.FromResult(Result.Success(svg));
    }
}
