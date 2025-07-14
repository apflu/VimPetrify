using Apflu.VimPetrify.Exterior;
using RimWorld;
using System.IO; // For file operations if saving/loading textures
using UnityEngine;
using Verse;

namespace Apflu.VimPetrify.Graphics
{
    // 自定义的Graphic类，用于渲染石化Pawn的雕像
    public class Graphic_PetrifiedPawn : Graphic_Single
    {
        // 存储原始Pawn的引用，这样我们才能获取其外观
        // 注意：这个Pawn不会被保存，因为它只是一个临时的引用，用于渲染
        // 实际的原始Pawn引用应该存储在BuildingPetrifiedPawnStatue的Comp中
        private Pawn originalPawnForRendering;

        // 雕像的石材颜色，现在从GraphicData获取
        // 我们会通过def.graphic.color来获取，或者使用默认值
        private Color stoneColor = new Color(0.5f, 0.5f, 0.5f); // 默认灰色

        // 材质的路径，用于应用石材纹理 (此变量目前未使用，因为我们直接用颜色着色Pawn纹理)
        // private string stoneTexturePath = "Things/Building/Linked/Rock_Smooth_Atlas"; 

        // Init方法现在用于在Graphic被创建后立即设置关联的Pawn和颜色
        // 它在Graphic实例化时或者从保存中加载时被调用
        public void Init(Pawn pawn, Color? colorOverride = null)
        {
            this.originalPawnForRendering = pawn;
            if (colorOverride.HasValue)
            {
                this.stoneColor = colorOverride.Value;
            }
            else
            {
                // 从父类的GraphicData中获取颜色
                // 注意：这里需要确保this.data已经通过Graphic.Init()方法设置
                // Graphic.Init()会在Graphic被ThingDef加载时自动调用
                if (this.data != null)
                {
                    this.stoneColor = this.data.color;
                }
                else
                {
                    Log.Warning($"[VimPetrify] Graphic_PetrifiedPawn: GraphicData is null during Init. Using default stone color.");
                    this.stoneColor = new Color(0.5f, 0.5f, 0.5f); // fallback
                }
            }

            // 清除旧的材质缓存，强制重新生成，以应用新的Pawn纹理和颜色
            this.mat = null;
        }

        // 或更推荐的方式是：在BuildingPetrifiedPawnStatue的SpawnSetup和PostDraw中使用Init方法。
        // 重写MatSingle，生成最终用于渲染的材质
        public override Material MatSingle
        {
            get
            {
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
                        reqDefault.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: false);
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

                    // 2. 将Pawn纹理灰度化并应用石材颜色
                    Texture2D finalTexture = null;
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
                        reqFallback.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: false);
                        if (reqFallback.mainTex == null) reqFallback.mainTex = BaseContent.BadTex;
                        reqFallback.shader = ShaderDatabase.Cutout;
                        reqFallback.color = this.stoneColor;
                        reqFallback.colorTwo = this.data.colorTwo;
                        mat = MaterialPool.MatFrom(reqFallback);
                        return mat;
                    }

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
            // 在这里获取thing，并尝试更新originalPawnForRendering
            // 这是确保MatSingle在获取材质时能够访问到正确Pawn的关键
            if (thing is BuildingPetrifiedPawnStatue statue && statue.PetrifiedComp != null)
            {
                // 如果originalPawnForRendering与当前Thing关联的Pawn不同，或者mat为空，则重新初始化
                // 这样可以确保MatSingle会重新生成材质
                if (this.originalPawnForRendering != statue.PetrifiedComp.originalPawn || this.mat == null)
                {
                    Init(statue.PetrifiedComp.originalPawn, this.data.color); // 使用graphicData的颜色初始化
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