using UnityEngine;
using UnityEngine.UI;
// using Sirenix.OdinInspector;
namespace NBShader
{
    [ExecuteAlways]
    public class NBShaderSpriteHelper : MonoBehaviour
    {
        public Sprite sprite;

        // [ReadOnly]
        public SpriteRenderer spRenderer;

        // [ReadOnly]
        public Image image;

        [InspectorButton("初始化", "Init")] public bool ButtomInspector;

        private Material mat;



        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        void Init()
        {
            if (TryGetComponent<SpriteRenderer>(out SpriteRenderer spr))
            {
                spRenderer = spr;
                sprite = spr.sprite;
            }
            else
            {
                if (TryGetComponent<Image>(out Image im))
                {
                    image = im;
                    sprite = im.sprite;
                }
            }



            if (sprite)
            {
                Texture texture = sprite.texture;
                Rect rect = sprite.textureRect;
                // sharedMaterial.SetVector("_BaseMapReverseST",CalScaleOffset(spRenderer.sprite.textureRect,spRenderer.sprite.texture));

                if (spRenderer)
                {
                    if (Application.isPlaying)
                    {
                        mat = spRenderer.material;
                    }
                    else
                    {
                        mat = spRenderer.sharedMaterial;
                    }
                }
                else if (image)
                {
                    mat = image.material;
                }

                if (mat && mat.shader.name == "Effects/NBShader")
                {
                    mat.SetVector("_MainTex_Reverse_ST", CalScaleOffset(rect, texture));
                    Debug.Log(mat.name);
                }
            }



        }

        Vector4 CalScaleOffset(Rect textureRect, Texture texture)
        {
            //这是一个反向的scale offset。
            //算法：如果原scale offset。转换后会是 scaleAfter= 1 / scale , offsetAfter = - offset/scale;
            Vector2 scaleAfter = new Vector2(texture.width / textureRect.width, texture.height / textureRect.height);
            Vector2 offsetAfter = new Vector2(-(textureRect.x / texture.width) * scaleAfter.x,
                -(textureRect.y / texture.height) * scaleAfter.y);
            Vector4 scaleOffset = new Vector4(scaleAfter.x, scaleAfter.y, offsetAfter.x, offsetAfter.y);
            return scaleOffset;
        }
    }
}
