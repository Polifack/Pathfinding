using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public Transform seeker, target;
    private Grid grid;

    private void Awake()
    {
        grid = GetComponent<Grid>();
    }
    private void Update()
    {
        FindPath(seeker.position, target.position);
    }
    private void FindPath(Vector3 startPos, Vector3 endPos)
    {
        //Cronometro para analizar el rendimiento del programa
        Stopwatch sw = new Stopwatch();
        sw.Start();


        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(endPos);

        //Implementación del algoritmo A*. 
        Heap<Node> openSet = new Heap<Node>(grid.maxSize);
        //List<Node> openSet = new List<Node>()

        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();

            closedSet.Add(currentNode);

            //Si el nodo en el que estamos es la meta, terminamos la exploración
            if (currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                sw.Stop();
                UnityEngine.Debug.Log("HEAP time: " + sw.ElapsedMilliseconds + " ms");
                return;
            }

            foreach (Node neighbour in grid.GetNeighbours(currentNode))
            {
                //Si el nodo no es atravesable o ya ha sido explorado, nos lo saltamos
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                //Calculamos el coste del camino hasta ese nodo
                int newPathToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);

                //Si el nodo no ha sido inicializado o se ha encontrado un camino más corto hasta ese nodo
                //actualizamos/inicializamos los valores de g cost y h cost en el nodo así como le asignamos un nodo padre
                if (newPathToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newPathToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
    }
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        //A la hora de calcular las distancias consideramos 10 para un desplazamiento recto y 14 en diagonal
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
    private void RetracePath(Node startNode, Node targetNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = targetNode;

        //Retrocedemos utilizando los padres para obtener la lista de nodos a visitar para obtener el camino a la meta
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        grid.path = path;
    }
}
