# ملفات الـSprite Sheets لإعادة التوليد

هذه الحزمة تحتوي على ملفات الـsprite sheets والأطالس المرئية المستخدمة في الإصدار الحالي من اللعبة. أُرسلت كما هي، بما في ذلك الزوائد أو الحواف غير المرغوبة، لتُستخدم كمرجع بصري عند إعادة توليد كل شخصية أو عنصر بصورة منفصلة.

> **المطلوب من النموذج الآخر:** لا يقتص من الصورة المرجعية ولا يعيد استخدام حوافها. عليه إنشاء asset جديد لكل عنصر مع **خلفية شفافة حقيقية (Alpha)**، من دون هالات بيضاء أو مربعات أو ظلال خارجية أو عناصر مقصوصة، وأن يترك هامشاً شفافاً بسيطاً حول العنصر.

## الشخصيات والأعداء

| الملف | المحتوى | المطلوب عند إعادة التوليد |
|---|---|---|
| `aster_idle_sheet.png` | وضعية سكون البطل Aster | بطل منفصل ونظيف، 4 وضعيات Idle متسقة. |
| `aster_run_sheet.png` | حركة ركض Aster | بطل منفصل، 6–8 وضعيات ركض سلسة. |
| `aster_jump.png` | قفزة Aster | بطل منفصل مع وضعيات صعود/سقوط واضحة. |
| `aster_attack_sheet.png` | هجوم السيف | بطل منفصل، 4–6 مراحل هجوم، والسيف يبقى في اليد نفسها. |
| `aster_hurt_sheet.png` | تلقي الضرر | بطل منفصل مع رد فعل قصير. |
| `aster_defeat_sheet.png` | هزيمة البطل | بطل منفصل مع مراحل هزيمة. |
| `moss_mask_crawler_sheet.png` | Moss Mask Crawler | عدو منفصل، 4–6 وضعيات تحرك/هجوم، مع حجم متوازن. |
| `ember_moth_sheet.png` | Ember Moth | عدو منفصل، 4–6 وضعيات رفرفة/اندفاع. |
| `enemy_archetype_motion_atlas.png` | ستة أنواع أعداء في أطلس واحد | أعد توليد **كل عدو كملف مستقل** بدلاً من أطلس مشترك. |
| `enemy_sheet.png` | ورقة أعداء أقدم | اختياري؛ مرجع إضافي فقط إذا أردت استبدال الأعداء القدامى. |
| `boss_motion_atlas.png` | ثلاثة زعماء في أطلس واحد | أعد توليد **كل زعيم كملف مستقل** مع idle/move/attack/hurt. |
| `boss_sheet.png` | ورقة زعماء أقدم | اختياري؛ مرجع إضافي فقط. |

## عناصر العالم والمقتنيات والمؤثرات

| الملف | المحتوى | المطلوب عند إعادة التوليد |
|---|---|---|
| `world_interactives_motion_atlas.png` | نقطة الحفظ، الأشواك، النار، الجليد، المنصات الهشة والجليدية | أنشئ **ملفاً منفصلاً لكل عنصر** مع قاعدة واضحة أسفل العنصر وشفافية نظيفة. |
| `collectibles_fx_motion_atlas.png` | عملات، جواهر، أسرار ومؤثرات جمع | أنشئ ملفاً مستقلاً لكل مقتنى أو تأثير. |
| `action_fx_motion_atlas.png` | trail الركض، الضربة، sparkle العملة | أنشئ ملفاً مستقلاً لكل تأثير. احرص أن أثر الركض له اتجاه واضح ويمكن قلبه أفقياً. |

## وصف مختصر جاهز لإعادة التوليد

استخدم هذا النص مع كل ملف مرجعي، ثم بدّل اسم العنصر فقط:

```text
Use this image only as a visual reference. Create a new, clean, standalone 2D fantasy platformer game sprite of [ELEMENT NAME], not a crop of the reference. The sprite must have a true transparent background with clean alpha edges, no white or colored fringe, no checkerboard, no rectangular backdrop, no unwanted shadows, and no extra objects. Keep the complete subject inside the canvas with a small transparent margin on every side. Match the reference's overall painterly cartoon fantasy style, palette, silhouette, and viewing angle. For an animated sprite sheet, arrange [NUMBER] evenly sized frames in one horizontal row, with the subject centered consistently in every frame. Do not add any text, labels, borders, logos, ground plane, or background.
```

إذا أرسل النموذج الآخر كل عنصر كصورة منفصلة أو كـsprite sheet نظيف، أرسل الملفات هنا وسأقوم بربطها باللعبة مع الحفاظ على المقاسات والشفافية وتموضعها على المنصات.
