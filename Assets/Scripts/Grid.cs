﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public Transform playerPosition;
    public float nodeRadius;
    public Vector2 gridWorldSize;
    public LayerMask unwalkableLayer;
    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    private void OnDrawGizmos()
    {
        //Marco princpal de la grid
        //El valor de Y es 1 y el valor de Z es el gridWorld.y porque estamos trabajando en un espacio tridimensional.
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        //Dibujamos los nodos de la grid asignandoles un color segun sean 'walkables' o no
        if (grid != null)
        {
            Node playerNode = NodeFromWorldPoint(playerPosition.position);
            foreach (Node node in grid)
            {
                Gizmos.color = (node.walkable ? Color.white : Color.red);
                if (node == playerNode) Gizmos.color = Color.cyan;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter-.1f));

            }
        }
    }

    private void Start()
    {
        nodeDiameter = nodeRadius * 2;
        //Número de nodos que podemos colocar en la grid
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        CreateGrid();
    }

    private void CreateGrid()
    {
        //Creamos la nueva grid basandonos en el numero de nodos que caben
        grid = new Node[gridSizeX, gridSizeY];

        //Punto inicial a partir del cual calcular los nodos. Utilizaremos el inferior izquierdo. 
        //Usamos vector3.forward porque estamos trabajando en el eje Z al ser un espacio tridimensional
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 
            - Vector3.forward * gridWorldSize.y / 2;


        for(int x=0; x < gridSizeX; x++)
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

                //Finalmente con estos valores creamos el nuevo nodo y lo asignamos
                grid[x, y] = new Node(walkable, worldPoint);
            }
        }
    }


    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        //Para calcular la posición de un objeto respecto a una cuadricula tenemos que calcular en que % de la cuadricula está
        //Para encontrarlo simplemente hacemos una regla de 3 respecto al 50%
        float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;

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
}