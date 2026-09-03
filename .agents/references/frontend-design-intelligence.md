# Frontend Design Intelligence

Use this reference to turn a product brief into an intentional, implementable UI direction. It synthesizes decision patterns from UI UX Pro Max, Anthropic Frontend Design, Vercel Web Interface Guidelines, CKW Design, and Microsoft Frontend Design Review. Do not copy a source style mechanically; select and adapt a direction to the product, audience, content, brand, and existing system.

## 1. Build the design brief

Capture these inputs before selecting a style:

- product type and industry;
- primary audience and their environment;
- the view's single most important user task;
- content density and data complexity;
- brand assets and existing design system;
- supported devices, themes, locales, and accessibility needs;
- framework, component library, performance budget, and delivery constraints.

If a missing choice would materially alter an existing product, present a compact recommendation and obtain approval. For a new surface with no established brand, choose a defensible direction and state the assumption.

## 2. Select a design direction

Score candidate directions from 1–5 on domain fit, task clarity, brand consistency, accessibility, implementation feasibility, and distinctiveness. Reject any candidate that fails task clarity or accessibility even when it looks memorable.

Choose one primary direction and, at most, one supporting influence. Avoid mixing several fashionable styles into an incoherent interface.

| Direction | Works well for | Core characteristics | Avoid when |
|---|---|---|---|
| Precise minimal | Enterprise tools, settings, focused workflows | Quiet surfaces, strong type scale, disciplined spacing, restrained accents | The brand needs high energy or rich storytelling |
| Data-dense utilitarian | Operations, finance, analytics, admin systems | Compact rhythm, tabular numerals, persistent context, clear status semantics | Marketing pages or emotionally led products |
| Modular bento | Dashboards, product overviews, mixed content | Unequal modules, clear grouping, responsive priority | Every item has equal importance or becomes repetitive cards |
| Editorial | Publishing, portfolios, premium commerce, thought leadership | Expressive type, narrative pacing, asymmetric composition, strong imagery | High-frequency operational tasks |
| Warm humanist | Healthcare, education, hospitality, food, community products | Natural palette, readable humanist type, soft geometry, reassuring copy | Precision-first technical or trading interfaces |
| Luxury restraint | Premium services, fashion, beauty, architecture | Generous space, controlled contrast, refined typography, minimal ornament | Dense workflows or value-focused mass-market products |
| Bold geometric | Technology launches, creative tools, youth brands | Strong shapes, confident color blocks, graphic type, one memorable motif | Conservative regulated products without brand approval |
| Neo-brutalist | Campaigns, experimental products, cultural events | Exposed structure, hard contrast, direct typography, purposeful roughness | Accessibility-sensitive or trust-sensitive products unless carefully moderated |
| Industrial/systemic | Developer tools, infrastructure, logistics | Grid logic, technical labels, functional color, visible system state | Lifestyle or emotionally warm products |
| Retro-tech | Games, media, developer culture, nostalgic campaigns | Era-specific type and controls, limited palette, purposeful pixel/terminal cues | Generic SaaS or when nostalgia conflicts with usability |
| Playful expressive | Consumer apps, children, events, creator products | Characterful illustration, lively shapes, friendly motion, conversational copy | Serious financial, medical, or safety-critical workflows |
| Cinematic dark | Media, music, gaming, premium launches | Deep surfaces, controlled highlights, dramatic imagery, spatial transitions | Text-heavy productivity work or weak display conditions |
| Glass/layered | Media controls, focused overlays, premium dashboards | Translucent layers, depth, backdrop separation, sparse accents | Dense pages, low-powered devices, or contrast cannot be guaranteed |
| Soft dimensional | Wellness, onboarding, friendly consumer utilities | Gentle depth, rounded forms, calm palette, tactile controls | Large data tables or interfaces needing sharp information boundaries |

Styles are not checklists. For example, bento is a grouping strategy, glass is a material treatment, and dark mode is a theme; none is a complete product identity by itself.

## 3. Create the visual system

Produce a compact UI contract before implementation:

### Palette

- Define 4–6 named semantic colors: canvas, surface, text, muted, primary, accent/status.
- Establish light/dark and interaction variants only when the product supports them.
- Use one dominant color relationship and reserve strong accents for priority or state.
- Verify text, controls, borders, charts, and status colors for usable contrast.
- Never rely on color alone to convey meaning.

### Typography

- Assign explicit roles: display/heading, body/UI, and optional mono/data.
- Define a small type scale with deliberate size, weight, line height, tracking, and measure.
- Use characterful type selectively; body text must remain readable under real density and localization.
- Use tabular numerals for columns or values that users compare.
- Prevent font loading from causing avoidable layout shifts or prolonged invisible text.

### Spacing and layout

- Choose a base spacing rhythm and reuse it consistently instead of arbitrary values.
- Define container widths, grid, gutters, section rhythm, density, and responsive collapse rules.
- Use hierarchy, alignment, proximity, and whitespace to encode relationships.
- Let asymmetry or grid-breaking support the concept, never obscure reading order or interaction.
- Account for long text, empty data, safe areas, fixed navigation, and small screens.

### Shape, depth, and imagery

- Define radius, border, shadow, elevation, and image treatment as tokens.
- Keep one coherent material language; do not mix soft neumorphism, hard brutalism, glass, and heavy shadows without a product reason.
- Verify official logos and use a consistent SVG icon family. Use text or icons, not emoji, for functional controls unless emoji is actual user content.

### Motion

- State the purpose of each motion pattern: orientation, feedback, continuity, emphasis, or storytelling.
- Prefer one orchestrated signature moment over unrelated effects everywhere.
- Animate compositor-friendly properties where possible and keep interaction feedback interruptible.
- Provide reduced or disabled variants for reduced-motion preferences.

## 4. Choose the component strategy

Use this order:

1. Reuse an existing approved component and its tokens.
2. Compose existing primitives into the required pattern.
3. Extend a component with a documented variant when consistent with its API.
4. Create a new component only when the existing system cannot express the requirement and the scope permits it.

React, Next.js, Vue, Tailwind, and shadcn are implementation tools, not aesthetic directions. Do not introduce or replace a stack to obtain a visual style. Keep variants, states, responsiveness, accessibility, and content behavior explicit in component APIs.

## 5. Prevent generic AI-looking UI

Before coding, reject or justify:

- the same fashionable font, purple gradient, dark neon accent, or cream editorial palette used regardless of domain;
- a centered hero followed by three equal cards without content-driven hierarchy;
- decorative pills, numbered labels, gradients, grids, or blobs that encode no information;
- excessive rounded cards enclosing every piece of content;
- generic copy, invented metrics, fake testimonials, and decorative dashboards;
- motion on every element, hover scaling that shifts layout, or effects that compete with the primary task.

Ask: “Could this exact design belong to an unrelated product?” If yes, revise the palette, typography, structure, content, or signature element until the direction is product-specific.

## 6. Design all interface states

Cover applicable states before declaring the component complete:

- default, hover, active, focus-visible, selected, disabled;
- loading, empty, partial, success, warning, and error;
- short, normal, long, translated, missing, and user-generated content;
- signed-out, insufficient permission, offline, and stale data where relevant;
- desktop, tablet, narrow mobile, zoomed text, reduced motion, and supported themes.

Error states must explain the next action. Empty states must orient the user and offer a relevant action rather than merely filling space.

## 7. Required design artifact

Before implementation, retain a compact design contract in the task or project artifact:

```text
Product / audience / primary task:
Direction and rationale:
Palette tokens:
Typography roles:
Spacing and layout system:
Component strategy:
Signature element:
Motion policy:
Responsive behavior:
Accessibility and performance constraints:
```
