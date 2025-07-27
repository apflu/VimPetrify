using Apflu.VimPetrify.Exterior;
using RimWorld;
using System.IO; // For file operations if saving/loading textures
using UnityEngine;
using Verse;

namespace Apflu.VimPetrify.Exterior
{
    // TODO: It is not working currently, thus skipped for now!
    public class Graphic_PetrifiedPawn : Graphic_Single
    {
       
        private Pawn originalPawnForRendering;

        private Color stoneColor = new Color(0.5f, 0.5f, 0.5f); 



        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            if (data != null)
            {
                stoneColor = data.color; 
            }

            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn.Init called. Initial stoneColor: {stoneColor}. Mat: {this.mat?.name ?? "null"}");
        }

        public void SetOriginalPawn(Pawn pawn, Color? colorOverride = null)
        {
            this.originalPawnForRendering = pawn;
            if (colorOverride.HasValue)
            {
                this.stoneColor = colorOverride.Value;
            }
            else if (this.data != null)
            {
                this.stoneColor = this.data.color;
            }

            
            this.mat = null;
            _ = MatSingle; 
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn.SetOriginalPawn called for {pawn?.Name.ToStringShort ?? "N/A"}. Mat will be regenerated.");
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn.SetOriginalPawn Mat: {this.mat?.name ?? "null"}");
        }

        public override Material MatSingle
        {
            get
            {
                Log.Message("test");
                Log.Message($"[VimPetrify] Graphic_PetrifiedPawn: MatSingle called for {this.data?.texPath ?? "unknown texture"} with originalPawnForRendering: {originalPawnForRendering?.Name.ToStringShort ?? "null"}");

                if (mat == null)
                {
                    // make sure Graphic_PetrifiedPawn.Init() called before this
                    if (originalPawnForRendering == null)
                    {
                        Log.Warning($"[VimPetrify] Graphic_PetrifiedPawn: originalPawnForRendering is null in MatSingle. Cannot generate pawn-specific material. Using default graphic.");
                        MaterialRequest reqDefault = default(MaterialRequest);
                        reqDefault.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: true);
                        if (reqDefault.mainTex == null) reqDefault.mainTex = BaseContent.BadTex; // Fallback to bad texture
                        reqDefault.shader = ShaderDatabase.Cutout;
                        reqDefault.color = this.stoneColor;
                        reqDefault.colorTwo = this.data.colorTwo;
                        mat = MaterialPool.MatFrom(reqDefault);
                        return mat;
                    }

                    Vector2 textureSize = new Vector2(128f, 128f);
                    Rot4 pawnRotation = originalPawnForRendering.Rotation;
                    Vector3 cameraOffset = new Vector3(0f, 0f, 0.1f);
                    float cameraZoom = 1.0f;
                    PawnHealthState? healthStateOverride = PawnHealthState.Mobile;

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

                        MaterialRequest reqFallback = default(MaterialRequest);
                        reqFallback.mainTex = ContentFinder<Texture2D>.Get(this.data.texPath, reportFailure: true);
                        if (reqFallback.mainTex == null) reqFallback.mainTex = BaseContent.BadTex;
                        reqFallback.shader = ShaderDatabase.Cutout;
                        reqFallback.color = Color.magenta; 
                        reqFallback.colorTwo = Color.black;
                        mat = MaterialPool.MatFrom(reqFallback);
                        return mat;
                    }

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
                                    pixel.a // origin alpha
                                );
                                finalTexture.SetPixel(x, y, mixedColor);
                            }
                        }
                        finalTexture.Apply();
                        Object.Destroy(tempTexture);
                    }
                    else
                    {
                        Log.Error($"[VimPetrify] Graphic_PetrifiedPawn: Could not get RenderTexture for pawn {originalPawnForRendering.Name.ToStringShort}. Using default graphic.");

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

                    MaterialRequest req = default(MaterialRequest);
                    req.mainTex = finalTexture;
                    req.shader = ShaderDatabase.Cutout;
                    req.color = this.data.color;
                    req.colorTwo = this.data.colorTwo;


                    mat = MaterialPool.MatFrom(req);
                }
                return mat;
            }
        }


        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Log.Message($"[VimPetrify] Graphic_PetrifiedPawn: DrawWorker called for {thing.LabelCap} at {loc} with rotation {rot}.");


            if (thing is BuildingPetrifiedPawnStatue statue && statue.PetrifiedComp != null)
            {

                if (this.originalPawnForRendering != statue.PetrifiedComp.originalPawn || this.mat == null)
                {
                    Log.Message($"[VimPetrify] DrawWorker: Updating originalPawnForRendering to {statue.PetrifiedComp.originalPawn?.Name.ToStringShort ?? "NULL"}.");
                    SetOriginalPawn(statue.PetrifiedComp.originalPawn, this.data.color); 
                }
            }
            else
            {
                Log.Warning($"[VimPetrify] Graphic_PetrifiedPawn: DrawWorker called with non-BuildingPetrifiedPawnStatue or null PetrifiedComp for {thing.LabelCap}.");
            }


            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
    }
}