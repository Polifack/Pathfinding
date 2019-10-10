using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    private PathRequestManager requestManager;
    private Grid grid;

    private void Awake()
    {
        grid = GetComponent<Grid>();
        requestManager = GetComponent<PathRequestManager>();
    }

    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        //Función que nos permite ejecutar una busqueda de caminos en paralelo mediante corrutinas.
        StartCoroutine(FindPath(startPos, targetPos));
    }
    private IEnumerator FindPath(Vector3 startPos, Vector3 endPos)
    {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(endPos);

        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        if (!startNode.walkable || !targetNode.walkable)
        {
            Debug.LogWarning("WARNING: Target or Start node are not walkable");
            yield return null;
        }

        //Implementación del algoritmo A*. 
        Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
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
                pathSuccess = true;

                break;
            }

            foreach (Node neighbour in grid.GetNeighbours(currentNode))
            {
                //Si el nodo no es atravesable o ya ha sido explorado, nos lo saltamos
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                //Calculamos el coste del camino hasta ese nodo (añadiendo la penalización del movimiento por terreno de dicho nodo)
                int newPathToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;

                //Si el nodo no ha sido inicializado o se ha encontrado un camino más corto hasta ese nodo
                //actualizamos/inicializamos los valores de g cost y h cost en el nodo así como le asignamos un nodo padre
                if (newPathToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newPathToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                    else openSet.UpdateItem(neighbour);
                }
            }
        }

        yield return null;

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
            requestManager.FinishedProcessingPath(waypoints, pathSuccess);
        }
        else
        {
            Debug.LogWarning("WARNING: No path has been found");
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
    private Vector3[] RetracePath(Node startNode, Node targetNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = targetNode;

        //Retrocedemos utilizando los padres para obtener la lista de nodos a visitar para obtener el camino a la meta
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;
    }
    private Vector3[] SimplifyPath(List<Node> path)
    {
        //Para mejorar la eficiencia de un path lo simplificamos para que solo se marquen los puntos importantes
        //Consideramos los puntos relevantes aquellos en los que el camino cambia de direcci´n
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 dirOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            //La nueva dirección se calcula como el vector que une los puntos de dos nodos consecutivos en el camino
            Vector2 dirNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (dirNew != dirOld)
            {
                waypoints.Add(path[i].worldPosition);
            }
            dirOld = dirNew;
        }

        return waypoints.ToArray();
    }
}
