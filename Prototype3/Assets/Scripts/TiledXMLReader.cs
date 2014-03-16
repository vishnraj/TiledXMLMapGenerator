using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.IO;

public class TiledXMLReader : MonoBehaviour 
{
    /*************************************************************************************************
    This tile map builder is only for 2D games and requires maps are orthogonal in orientation. 
    
    The way this script works:
    
    Attatch this script to a gameobject. Next, using the Tiled Editor (free doownload available at http://www.mapeditor.org/) build a map. Follow the documentation on
    Tiled's website to make sure that you save your map as an xml file. The Tiled file will then be available as a tmx file, however if you look inside, you will 
    notice that it is just an xml file (the tmx file can be saved under multiple different formats, but for this tool it must be in xml). 
    
    Next, once you look at the inspector for this tool you will notice there are 3 different URLs that must be entered. The first one is the link
    to your tmx file that you created earlier. The second is a link to the FOLDER (not a texture, the script creates these for each tile) that will hold all the tile
    textures. The third is the link to the scene that will contain your map. Note, you don't need to have a scene there, since unity will make one. 
    
    Next, depending on the pictures you used to be build your map, make sure those pictures are saved in your textures folder. Then, select those textures and drag them
    into the texture list in the inspector. Name each texture in the corresponding index in the texture names list. It is important that these names correspond with
    the indexes of the respective texture in the texture list and the names of the tilesets in the tiled editor.
    
    Next, make sure that the tile size (the pixel x pixel count in tiled) is sepcified. Finally, make a prefab (refer to the demo if needed) that reprsents an 
    individual tile object. 
    
    Finally, click play. You may have to wait for some time as the tiles are loaded. Leaving scene and coming back to it will allow you to see the progress as the
    tiles are being uploaded to the tile folder. Once this is done, a new scene should appear containing the tiles saved in the same order as they appear on your
    map in the tiled editor. 
    
    Some features not available in this reader is a replication of objects that can be made in Tiled. This can be done in unity once the map is loaded and seems to be
    more accurate that doing this in Tiled. Also only 1 layer is supported in this reader, so if you wish to generate multiple layers, you can edit this script
    or make each map for each different layer and create the seperate layers in unity.
    
    IMPORTANT: Make sure to change textures to read/write enabled on the editor. This can be done by going to each texture, going to inspector,
    then changing texture type to advanced, and then checking the read/write enabled box. Also textures must be the same the same width and height 
    as that of the corresponding image used in Tiled. Refer to the tmx file and look at image height and width if needed. To resize an image, you 
    can either use unity's editor by clicking on the texture and going to the inspector and changing the dimensions under max size, or by using 
    an external resizing program (or just doing it manually).
     
    *************************************************************************************************/

    static XmlReader reader; //reads in xml file
    public int tileSize; //sets the tile size in editor
    public string xmlSourceUrlName; //sets path to tmx file
    public string destinationUrlName; //sets path to tiles folder
    public string sceneDestinationUrlName; //sets path to scene that contains map
    public List<string> textureNames; //holds texture/tileset names
    public List<Texture2D> Textures; //holds textures that were used to make tiles

    public GameObject tile; //for prefab that represents each different tile

    int mapWidth; //width of map
    int mapHeight; //height of map
    int tileWidth; //the width of each tile
    int tileHeight; //the height of each tile

    //Class for tilesets
    class tileset
    {
        public int firstGid;
        public string name;
        public string source;
        public int tileWidth;
        public int tileHeight;
        public int imageWidth;
        public int imageHeight;

        public tileset() { }

        public tileset(int fGid, string n, int tWidth, int tHeight, string s, int iWidth, int iHeight)
        {
            firstGid = fGid;
            name = n;
            tileWidth = tWidth;
            tileHeight = tHeight;
            source = s;
            imageWidth = iWidth;
            imageHeight = iHeight;
        }
    }

    List<tileset> tilesets = new List<tileset>(); //list for tilesets from the tmx file

    //Creates tilesets from the tmx file
    void constructTileSets (XmlDocument doc)
    {
        XmlAttributeCollection mapAttributes = doc.DocumentElement.Attributes;

        mapWidth = Convert.ToInt32(mapAttributes["width"].Value);
        mapHeight = Convert.ToInt32(mapAttributes["height"].Value);
        tileWidth = Convert.ToInt32(mapAttributes["tilewidth"].Value);
        tileHeight = Convert.ToInt32(mapAttributes["tileheight"].Value);

        XmlNodeList children = doc.DocumentElement.ChildNodes;

        int i = 0;
        while (children[i].Name == "tileset")
        {
            XmlAttributeCollection tilesetAttributes = children[i].Attributes;

            int first_gid = Convert.ToInt32(tilesetAttributes["firstgid"].Value);
            string n = tilesetAttributes["name"].Value;
            int tile_width = Convert.ToInt32(tilesetAttributes["tilewidth"].Value);
            int tile_height = Convert.ToInt32(tilesetAttributes["tileheight"].Value);
            string s = children[i].FirstChild.Attributes["source"].Value;
            int image_width = Convert.ToInt32(children[i].FirstChild.Attributes["width"].Value);
            int image_height = Convert.ToInt32(children[i].FirstChild.Attributes["height"].Value);

            tileset next_tileset = new tileset(first_gid, n, tile_width, tile_height, s, image_width, image_height);

            tilesets.Add(next_tileset);
            ++i;
        }
    }

    //Constructs map from tmx file and saves map in a specified scene 
    void constructMap (XmlDocument doc)
    {
        XmlNode root = doc.DocumentElement;
        XmlNode dataStart = root["layer"].FirstChild;
        XmlNodeList grid = dataStart.ChildNodes;

        float xpos = 0;
        float ypos = 0;
        float spacingFactor = tileSize * .01f;
        for (int i = 0; i < grid.Count; ++i)
        {
            int gid = Convert.ToInt32(grid[i].Attributes["gid"].Value);
            tileset currentTileSet = new tileset();

            bool stop = false;
            int j = 0;
            while (!stop)
            {
                if (j == tilesets.Count - 1)
                {
                    currentTileSet = tilesets[j];
                    stop = true;
                }

                else if (gid >= tilesets[j].firstGid && gid < tilesets[j + 1].firstGid)
                {
                    currentTileSet = tilesets[j];
                    stop = true;
                }

                else
                {
                    ++j;
                }
            }

            for (int z = 0; z < textureNames.Count; ++z)
            {
                if (textureNames[z] == currentTileSet.name)
                {
                    Texture2D currentTexture = Textures[z];
                    
                    int normalizedGid = gid - (currentTileSet.firstGid - 1);
                    int columnSize = currentTileSet.imageWidth / currentTileSet.tileWidth;
                    int rowSize = currentTileSet.imageHeight / currentTileSet.tileHeight;
                    int column = normalizedGid % columnSize;
                    int row = 1;

                    if (column != 0)
                    {
                        row = rowSize - ((normalizedGid + (columnSize - column)) / columnSize) + 1;
                    }

                    else
                    {
                        column = columnSize;
                        row = rowSize - (normalizedGid / columnSize) + 1;  
                    }
                    
                    int x = (column - 1) * tileWidth;
                    int y = (row - 1) * tileHeight;
                    int blockWidth = currentTileSet.tileWidth;
                    int blockHeight = currentTileSet.tileHeight;

                    Color[] tilePixels = currentTexture.GetPixels(x, y, blockWidth, blockHeight);

                    Texture2D destTex = new Texture2D(tileWidth, tileHeight);
                    destTex.SetPixels(tilePixels);
                    destTex.Apply();

                    File.WriteAllBytes(destinationUrlName + "/" + Convert.ToString(i) + ".png", (byte[])destTex.EncodeToPNG());

                    tile.GetComponent<SpriteRenderer>().sprite = Sprite.Create(destTex, new Rect(0, 0, tileWidth, tileHeight), new Vector2(0, 0));
                    Instantiate(tile, new Vector3(xpos, ypos, 0), Quaternion.identity);
                }
            }

            xpos += spacingFactor;
            
            if ((i + 1) % mapWidth == 0)
            {
                xpos = 0;
                ypos -= spacingFactor;
            }
        }
    }

	// Use this for initialization
	void Start () 
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        reader = XmlReader.Create(xmlSourceUrlName, settings);

        XmlDocument doc = new XmlDocument();
        doc.Load(reader);
        
        if (reader == null)
        {
            reader.Close();
            print("error: file could not be found"); 
        }

        else
        {
            constructTileSets(doc);
            constructMap(doc);

            EditorApplication.SaveScene(sceneDestinationUrlName, true);
        }
	}
	
	// Update is called once per frame
	void Update () 
    {
	
	}
}
