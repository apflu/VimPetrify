using Apflu.VimPetrify.Exterior;
using RimWorld;
using System.IO; // For file operations if saving/loading textures
using UnityEngine;
using Verse;

namespace Apflu.VimPetrify.Exterior
{
    // 自定义的Graphic类，用于渲染石化Pawn的雕像
    public class Graphic_PetrifiedPawn : Graphic_Single
    {
        // 实际的原始Pawn引用应该存储在BuildingPetrifiedPawnStatue的Comp中
        private Pawn originalPawnForRendering;

        private Color stoneColor = new Color(0.5f, 0.5f, 0.5f); // 默认灰色



        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            if (data != null)
            {
                stoneColor = data.color; // 从GraphicData中获取颜色
            }
            // 输出Init结束时的this.mat
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn.Init called. Initial stoneColor: {stoneColor}. Mat: {this.mat?.name ?? "null"}");
        }

        public void SetOriginalPawn(Pawn pawn, Color? colorOverride = null)
        {
            this.originalPawnForRendering = pawn;
            if (colorOverride.HasValue)
            {
                this.stoneColor = colorOverride.Value;
            }
            else if (this.data != null) // 确保this.data已设置
            {
                this.stoneColor = this.data.color;
            }

            // 清除旧的材质缓存，强制重新生成
            this.mat = null;
            _ = MatSingle; // 触发MatSingle的调用，确保材质被重新生成
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn.SetOriginalPawn called for {pawn?.Name.ToStringShort ?? "N/A"}. Mat will be regenerated.");
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn.SetOriginalPawn Mat: {this.mat?.name ?? "null"}");
        }

        // 重写MatSingle，生成最终用于渲染的材质
        public override Material MatSingle
        {
            get
            {
                Log.Message("test");
                Log.Message($"[VimPetrify] Graphic_PetrifiedPawn: MatSingle called for {this.data?.texPath ?? "unknown texture"} with originalPawnForRendering: {originalPawnForRendering?.Name.ToStringShort ?? "null"}");

                if (mat == null)
                {
                    // MatSingle在渲染时被调用。此时originalPawnForRendering必须已经设置。
                    // 确保在BuildingPetrifiedPawnStatue的SpawnSetup或PostLoad时调用Graphic_PetrifiedPawn.Init()
                    if (originalPawnForRendering == null)
                    {
                        Log.Warning($"[VimPetrify] Graphic_PetrifiedPawn: originalPawnForRendering is null in MatSingle. Cannot generate pawn-specific material. Using default graphic.");
                        // 如果originalPawnForRendering为空，我们只能返回一个通用的材质
                        // 可以选择返回一个默认的灰色方块材质，或者尝试使用def.graphic.texPath指定的纹理
                        MaterialRequest reqDefault = default(MaterialRequest);
                        reqDefault.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: true);
                        if (reqDefault.mainTex == null) reqDefault.mainTex = BaseContent.BadTex; // Fallback to bad texture
                        reqDefault.shader = ShaderDatabase.Cutout;
                        reqDefault.color = this.stoneColor;
                        reqDefault.colorTwo = this.data.colorTwo;
                        mat = MaterialPool.MatFrom(reqDefault);
                        return mat;
                    }

                    // 1. 获取Pawn的渲染纹理
                    // 这里的尺寸可以根据你的需求调整
                    Vector2 textureSize = new Vector2(128f, 128f);
                    Rot4 pawnRotation = originalPawnForRendering.Rotation; // 使用Pawn的当前朝向
                    Vector3 cameraOffset = new Vector3(0f, 0f, 0.1f);
                    float cameraZoom = 1.0f; // 默认缩放，可以根据需要调整
                    PawnHealthState? healthStateOverride = PawnHealthState.Mobile; // 渲染活着的Pawn状态

                    RenderTexture pawnRenderTexture = PortraitsCache.Get(
                        originalPawnForRendering,
                        textureSize,
                        pawnRotation,
                        cameraOffset,
                        cameraZoom,
                        supersample: true,
                        compensateForUIScale: true,
                        renderHeadgear: true,
                        renderClothes: true,
                        null, null, stylingStation: false, healthStateOverride
                    );

                    if (pawnRenderTexture == null)
                    {
                        Log.Error($"[VimPetrify] Graphic_PetrifiedPawn: PortraitsCache.Get returned null for pawn {originalPawnForRendering.Name.ToStringShort}. Is the pawn valid?");
                        // 返回一个可见的默认纹理，以便你看到它在游戏中显示
                        MaterialRequest reqFallback = default(MaterialRequest);
                        reqFallback.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: true);
                        if (reqFallback.mainTex == null) reqFallback.mainTex = BaseContent.BadTex;
                        reqFallback.shader = ShaderDatabase.Cutout;
                        reqFallback.color = Color.magenta; // 用一个醒目的颜色
                        reqFallback.colorTwo = Color.black;
                        mat = MaterialPool.MatFrom(reqFallback);
                        return mat;
                    }

                    // 2. 将Pawn纹理灰度化并应用石材颜色
                    Texture2D finalTexture = null;
                    Log.Message($"[VimPetrify] Graphic_PetrifiedPawn: Successfully got RenderTexture for pawn {originalPawnForRendering.Name.ToStringShort}. Processing texture...");
                    if (pawnRenderTexture != null)
                    {
                        RenderTexture.active = pawnRenderTexture;
                        Texture2D tempTexture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.ARGB32, false);
                        tempTexture.ReadPixels(new Rect(0, 0, textureSize.x, textureSize.y), 0, 0);
                        tempTexture.Apply();
                        RenderTexture.active = null;

                        finalTexture = new Texture2D(tempTexture.width, tempTexture.height, TextureFormat.ARGB32, false);
                        for (int x = 0; x < tempTexture.width; x++)
                        {
                            for (int y = 0; y < tempTexture.height; y++)
                            {
                                Color pixel = tempTexture.GetPixel(x, y);
                                float gray = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
                                Color mixedColor = new Color(
                                    gray * stoneColor.r,
                                    gray * stoneColor.g,
                                    gray * stoneColor.b,
                                    pixel.a // 保留原始alpha
                                );
                                finalTexture.SetPixel(x, y, mixedColor);
                            }
                        }
                        finalTexture.Apply();
                        Object.Destroy(tempTexture); // 销毁临时Texture2D
                    }
                    else
                    {
                        Log.Error($"[VimPetrify] Graphic_PetrifiedPawn: Could not get RenderTexture for pawn {originalPawnForRendering.Name.ToStringShort}. Using default graphic.");
                        // 如果无法获取Pawn纹理，回退到默认
                        MaterialRequest reqFallback = default(MaterialRequest);
                        reqFallback.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: true);
                        if (reqFallback.mainTex == null) reqFallback.mainTex = BaseContent.BadTex;
                        reqFallback.shader = ShaderDatabase.Cutout;
                        reqFallback.color = this.stoneColor;
                        reqFallback.colorTwo = this.data.colorTwo;
                        mat = MaterialPool.MatFrom(reqFallback);
                        return mat;
                    }

                    Log.Message($"[VimPetrify] Graphic_PetrifiedPawn: Successfully processed texture for pawn {originalPawnForRendering.Name.ToStringShort}.");
                    // 3. 构建材质请求
                    MaterialRequest req = default(MaterialRequest);
                    req.mainTex = finalTexture;
                    req.shader = ShaderDatabase.Cutout;
                    req.color = this.data.color; // 使用ThingDef中graphicData的颜色
                    req.colorTwo = this.data.colorTwo; // 使用ThingDef中graphicData的第二颜色

                    // 4. 从MaterialRequest创建材质
                    mat = MaterialPool.MatFrom(req);
                }
                return mat;
            }
        }

        // 重写DrawWorker，确保每次绘制时MatSingle都能获取到正确的originalPawnForRendering
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn: DrawWorker called for {thing.LabelCap} at {loc} with rotation {rot}.");

            // 在这里获取thing，并尝试更新originalPawnForRendering
            // 这是确保MatSingle在获取材质时能够访问到正确Pawn的关键
            if (thing is BuildingPetrifiedPawnStatue statue && statue.PetrifiedComp != null)
            {
                // 如果originalPawnForRendering与当前Thing关联的Pawn不同，或者mat为空，则重新初始化
                // 这样可以确保MatSingle会重新生成材质
                if (this.originalPawnForRendering != statue.PetrifiedComp.originalPawn || this.mat == null)
                {
                    Log.Message($"[VimPetrify] DrawWorker: Updating originalPawnForRendering to {statue.PetrifiedComp.originalPawn?.Name.ToStringShort ?? "NULL"}.");
                    SetOriginalPawn(statue.PetrifiedComp.originalPawn, this.data.color); // 使用graphicData的颜色
                }
            }
            else
            {
                Log.Warning($"[VimPetrify] Graphic_PetrifiedPawn: DrawWorker called with non-BuildingPetrifiedPawnStatue or null PetrifiedComp for {thing.LabelCap}.");
            }

            // 调用基类的DrawWorker，它会使用MatSingle来绘制
            // 此时MatSingle应该能够正确获取originalPawnForRendering并生成材质
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
    }
}