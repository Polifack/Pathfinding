using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public float nodeRadius;
    public bool showGrid;
    public int blurSize;
    public Vector2 gridWorldSize;
    public LayerMask unwalkableLayer;
    public TerrainType[] walkableRegions;
    private LayerMask walkableMask;
    private Dictionary<int, int> walkableRegionsHash = new Dictionary<int, int>();

    private Node[,] grid;

    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    private int penaltyMin = int.MaxValue;
    private int penaltyMax = int.MinValue;


    private void OnDrawGizmos()
    {
        //Marco princpal de la grid
        //El valor de Y es 1 y el valor de Z es el gridWorld.y porque estamos trabajando en un espacio tridimensional.
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        //Dibujamos los nodos de la grid asignandoles un color segun sean 'walkables' o no
        if (grid != null)
        {
            foreach (Node node in grid)
            {
                if (showGrid)
                {
                    //Obtenemos el color del nodo relativo a su peso 
                    Color weight = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax,
                        node.movementPenalty));

                    Gizmos.color = (node.walkable ? weight : Color.red);
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter));
                }
            }
        }
    }
    private void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        //Número de nodos que podemos colocar en la grid
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        /* Añadimos todas las Walkable Regions a la Layer Mask.
         * Hay que tener en cuenta que Unity guarda los indices de las layers en un Integer de 32 bits.
         * Por ejemplo la layer 9 sería 00000000 0000000 0000000 00000010 0000000
         * y la layer 10 sería 00000000 0000000 0000000 00000100 0000000
         * por lo que si queremos crear una LayerMask que contenga tanto la 9 como la 10 usamos el OR operator
         * para sumarlas (OR se representa como " | ") */

        foreach (TerrainType terrain in walkableRegions)
        {
            walkableMask.value |= terrain.terrainMask.value;

            //Para acceder rapidamente a cada una de las penalizaciones del terreno las storeamos en un hash
            walkableRegionsHash.Add((int)Mathf.Log(terrain.terrainMask.value, 2), terrain.terrainPenalty);
        }


        CreateGrid();
    }

    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }
    private void CreateGrid()
    {
        //Creamos la nueva grid basandonos en el numero de nodos que caben
        grid = new Node[gridSizeX, gridSizeY];

        //Punto inicial a partir del cual calcular los nodos. Utilizaremos el inferior izquierdo. 
        //Usamos vector3.forward porque estamos trabajando en el eje Z al ser un espacio tridimensional
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2
            - Vector3.forward * gridWorldSize.y / 2;


        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                //Cauculamos el punto del mundo en el que se encuentra el centro del nodo actual.
                //Para calcular este punto multiplicamos las iteraciones del loop por el tamaño del nodo y sumamos el radio.
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                //Comprobamos si dicho nodo es "Walkable". Para ello usamos una esfera de colisiones para comprobar si chocamos
                //con algun objeto que pertenezca a la layer designada como "unwalkable"
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableLayer));

                //Calculamos la penalización del movimiento de dicho nodo
                int movementPenalty = 0;
                if (walkable)
                {
                    Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                    RaycastHit hit;

                    //Hacemos el raycast pasando como referencia la variable 'hit' en la que se storean los datos de la colision
                    if (Physics.Raycast(ray, out hit, 100, walkableMask))
                    {
                        //Si nos encontramos con algun objeto intentamos asociar la layer de dicho objeto con el movementPenalty.
                        walkableRegionsHash.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                    }
                }

                //Finalmente con estos valores creamos el nuevo nodo y lo asignamos
                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }

        BlurWeights(blurSize);
    }
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        /*Para obtener los nodos vecinos vamos a considerar una cuadricula de 3x3:
         * | a | b | c |
         * | d | x | e |
         * | f | g | h |
         * donde x es el nodo 'node' y el resto son los vecinos. Para calcularla consideramos a x como (0,0)
         * y los nodos vecinos como variaciones entre (-1,-1), que vendría siendo f y (1,1) que vendría siendo c*/

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                //Consideramos que 0,0 es el nodo en al que le estamos calculando los vecinos, así que saltamos.
                if (x == 0 && y == 0) continue;

                //Obtenemos el nodo
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                //Comprobamos que los nodos se encuentran en la cuadricula
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        //Para calcular la posición de un objeto respecto a una cuadricula tenemos que calcular en que % de la cuadricula está
        //Para encontrarlo simplemente hacemos una regla de 3 respecto al 50%
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;

        //Nos aseguramos de que el valor devuelto está entre 0 y 1 para no calcular cosas fuera de la grid.
        //De esta forma nos evitamos obtener cosas fuera del array. 
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        //Obtenemos el index dentro del Array en el que se encuentra el porcentaje.
        //Acordarse de sustraer 1 al tamaño de los Arrays porque empiezan en 0.
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }
    public void BlurWeights(int blurSize)
    {
        //If blurSize = 1 entonces estamos comparando con las 2 casillas colindantes, por lo que la matriz de kernel 
        //Es de 3x3, o lo que es lo mismo 2*1+1
        int kernelSize = blurSize * 2 + 1;

        //Tamaño de los nodos que entran en el kernel durante cada iteración, por ejemplo si el kernel es de 3x3 cada
        //iteración va a entrar un nodo en el kernel (y salir otro)
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalPass = new int[gridSizeX, gridSizeY];

        //Atravesamos la matriz horizontalmente.
        for (int y = 0; y < gridSizeY; y++)
        {
            /* Calculamos los valores de la primera columna
             * para ello la parseamos en horizontal tantas veces como el tamaño del kernel extent
             * cada iteración vamos acumulando el valor, por lo que es lo mismo que si extendiesemos la matriz
             * EJ: Si kernel extend = 1 tendriamos que sumar 2 veces el valor del borde, entonces pasamos 2 veces por 
             * el bucle
             */
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            //Calcualmos el resto de valores simplemente desplazando el kernel horizontalmente
            for (int x = 1; x < gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                //El valor de x, y va a ser el valor de x-1, y - penalizacion eliminado + penalizacion añadido
                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty +
                    grid[addIndex, y].movementPenalty;
            }

        }

        //Atravesamos la matriz verticalmente.
        for (int x = 0; x < gridSizeX; x++)
        {
            //Hacemos la acumulación del valor directamente en la matriz, sumando los valoers a lo obtenido en el parseo por filas.
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            //Asignamos el valor de desenfoque de la primera fila
            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = blurredPenalty;

            //Calcualmos el resto de valores simplemente desplazando el kernel horizontalmente
            for (int y = 1; y < gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                //El valor de x, y va a ser el valor de x-1, y - penalizacion eliminado + penalizacion añadido
                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] +
                    penaltiesHorizontalPass[x, addIndex];

                //Una vez que se acabe este bucle consideramos que ya se han obtenido todos los datos, entonces pasamos a calcular
                //el valor definitivo del desenfoque

                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));

                //Asignamos el valor de desenfoque
                grid[x, y].movementPenalty = blurredPenalty;

                //Guardamos los valores maximos y minimos de las penalizaciones para poder dibujarlas en el gizmos
                if (blurredPenalty > penaltyMax)
                {
                    penaltyMax = blurredPenalty;
                }
                if (blurredPenalty < penaltyMin)
                {
                    penaltyMin = blurredPenalty;
                }
            }

        }
    }
}

[System.Serializable]
public class TerrainType
{
    public LayerMask terrainMask;
    public int terrainPenalty;
}
