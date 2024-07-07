using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPainter : MonoBehaviour
{
    [SerializeField] private Mapper mapper;

    private float[,] map;

    public RenderTexture renderTexture; // renderTextuer that you will be rendering stuff on
    public Renderer renderer; // renderer in which you will apply changed texture
    Texture2D texture;


    // Start is called before the first frame update
    void Start()
    {
        texture = new Texture2D(renderTexture.width, renderTexture.height);
        renderer.material.mainTexture = texture;
    }

    // Update is called once per frame
    void Update()
    {
        map = mapper.map2D;

        RenderTexture.active = renderTexture;
        //don't forget that you need to specify rendertexture before you call readpixels
        //otherwise it will read screen pixels.
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                texture.SetPixel(i, j, new Color(map[i, j], 0, 0));

                if (mapper.GetMapCenter().x == j && mapper.GetMapCenter().x == i)
                    texture.SetPixel(i, j, new Color(0, 0, 1));
            }
        }
        texture.Apply();
        RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it. 
    }
}
