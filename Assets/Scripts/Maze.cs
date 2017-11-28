﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour {

    public MazeCell cellPrefab;
    public MazeCell[,] cells;
    public MazePassage passagePrefab;
    public MazeWall mazeWallPrefab;
    public MazeDoor doorPrefab;

    [Range(0f, 1f)]
    public float doorProbability;

    public float generateDelay;
    public IntVector2 size;

    private List<MazeRoom> rooms = new List<MazeRoom>();
    public MazeRoomSettings[] roomSettings;

    public MazeCell getCell (IntVector2 coordinates)
    {
        return cells[coordinates.x, coordinates.z];
    }

    public IEnumerator Generate()
    {
        cells = new MazeCell[size.x, size.z];

        List<MazeCell> activeCells = new List<MazeCell>();
        DoFirstGenerationStep(activeCells);

        WaitForSeconds delay = new WaitForSeconds(generateDelay);

        while (activeCells.Count > 0)
        {

            yield return delay;
            DoNextGenerationStep(activeCells);


        }

        IntVector2 coordinates = RandomCoordinates;

        while(ContainsCoordinates(coordinates) && getCell(coordinates) == null)
        {
            yield return delay;
            CreateCell(coordinates);
            coordinates += MazeDirections.randomValue.ToIntVector2();
        }
    }

    private MazeCell CreateCell(IntVector2 coordinates)
    {
        MazeCell newRoof = Instantiate(cellPrefab) as MazeCell;
        MazeCell newCell = Instantiate(cellPrefab) as MazeCell;
        cells[coordinates.x, coordinates.z] = newCell;
        newCell.coordinates = coordinates;
        newCell.name = "Maze Cell " + coordinates.x + ", " + coordinates.z;
        newCell.transform.parent = transform;
        newCell.transform.localPosition = new Vector3(coordinates.x - size.x * 1f + 1f, 0f, coordinates.z - size.z * 1f + 1f);
        newRoof.coordinates = coordinates;
        newRoof.name = "Maze Cell Roof" + coordinates.x + ", " + coordinates.z;
        newRoof.transform.parent = transform;
        newRoof.transform.localPosition = new Vector3(coordinates.x - size.x * 1f + 1f, 2f, coordinates.z - size.z * 1f + 1f);
        return newCell;
    }

    private void DoFirstGenerationStep(List<MazeCell> activeCells)
    {
        MazeCell newCell = CreateCell(RandomCoordinates);
        newCell.Initialize(CreateRoom(-1));
        activeCells.Add(newCell);
    }

    private void CreatePassageInSameRoom(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazePassage passage = Instantiate(passagePrefab) as MazePassage;
        passage.Initialize(cell, otherCell, direction);
        passage = Instantiate(passagePrefab) as MazePassage;
        passage.Initialize(otherCell, cell, direction.GetOpposite());
    }

    private void DoNextGenerationStep(List<MazeCell> activeCells)
    {
        int currentIndex = activeCells.Count - 1;
        MazeCell currentCell = activeCells[currentIndex];

        if (currentCell.IsFullyInitialized)
        {
            activeCells.RemoveAt(currentIndex);
            return;
        }
        MazeDirection direction = currentCell.RandomUninitializedDirection;
        IntVector2 coordinates = currentCell.coordinates + direction.ToIntVector2();
        if (ContainsCoordinates(coordinates))
        {
            MazeCell neighbor = getCell(coordinates);
            if (neighbor == null)
            {
                neighbor = CreateCell(coordinates);
                CreatePassage(currentCell, neighbor, direction);
                activeCells.Add(neighbor);
            }

            else if (currentCell.room.settingsIndex == neighbor.room.settingsIndex)
            {
                CreatePassageInSameRoom(currentCell, neighbor, direction);
            }
            else
            {
                CreateWall(currentCell, neighbor, direction);
                
            }
        }
        else
        {
            CreateWall(currentCell, null, direction);
            
        }
    }

    private void CreatePassage(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazePassage prefab = Random.value < doorProbability ? doorPrefab : passagePrefab;
        MazePassage passage = Instantiate(prefab) as MazePassage;

        passage.Initialize(cell, otherCell, direction);
        //passage = Instantiate(prefab) as MazePassage;

        if (cell.room != otherCell.room && otherCell.room != null)
        {
            MazeRoom roomToAssimilate = otherCell.room;
            cell.room.Assimilate(roomToAssimilate);
            rooms.Remove(roomToAssimilate);
            Destroy(roomToAssimilate);
        }

        if (passage is MazeDoor)
        {
            
            otherCell.Initialize(CreateRoom(cell.room.settingsIndex));
        }
        else
        {
            otherCell.Initialize(cell.room);
        }

        passage.Initialize(otherCell, cell, direction.GetOpposite());
    }

    private void CreateWall(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazeWall wall = Instantiate(mazeWallPrefab) as MazeWall;
        wall.Initialize(cell, otherCell, direction);
        if (otherCell != null)
        {
            wall = Instantiate(mazeWallPrefab) as MazeWall;
            wall.Initialize(otherCell, cell, direction.GetOpposite());
        }
    }

    private MazeRoom CreateRoom(int indexToExclude)
    {
        MazeRoom newRoom = ScriptableObject.CreateInstance<MazeRoom>();
        newRoom.settingsIndex = Random.Range(0, roomSettings.Length);
        if (newRoom.settingsIndex == indexToExclude)
        {
            newRoom.settingsIndex = (newRoom.settingsIndex + 1) % roomSettings.Length;
        }
        newRoom.settings = roomSettings[newRoom.settingsIndex];
        rooms.Add(newRoom);
        return newRoom;
    }

    public IntVector2 RandomCoordinates
    {
        get
        {

            return new IntVector2(Random.Range(0, size.x), (Random.Range(0, size.z)));
        }

    }

    public bool ContainsCoordinates (IntVector2 coordinate)
    {

        return coordinate.x >= 0 && coordinate.x < size.x && coordinate.z >= 0 && coordinate.z < size.z;
    }

}
