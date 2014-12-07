using UnityEngine;
using uLink;
using System;
using System.Collections;
using System.Collections.Generic;

public class CharacterControl : uLink.MonoBehaviour {
    [Serializable]
    public class QueueEntry
    {
        public enum State
        {
            NotProcessed,
            Processing,
            DoneProcessing
        }
        public enum EntryType
        {
            Move,
            Attack,
        }
        public State state;
        public EntryType type;
        public Vector3Int direction;
        
        public Vector3Int CurrentLocation;
        public Vector3Int NextLocation;

        public float interPos;

        public QueueEntry(Vector3Int _dir, EntryType _type)
        {
            direction = _dir;
            type = _type;
            state = State.NotProcessed;

            CurrentLocation = new Vector3();
            NextLocation = new Vector3();

            interPos = 0;
        }
    }

    public float moveSpeed = 0.00f;
    public List<QueueEntry> MoveQueue = new List<QueueEntry>();
    public QueueEntry current;
    public int queueIndex = 0;
    public uLink.NetworkView netview;
    public LayerMask mask;
    public float maxHealth = 100;
    public float health = 0;
    public float AttackAmount = 5;

    private float lastSyncTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition = Vector3.zero;
    private Vector3 syncEndPosition = Vector3.zero;

	// Use this for initialization
	void Start () {
        netview = uLink.NetworkView.Get(this);
        if (netview.isMine)
        {
            LuaManager.AddToGlobalInstance(this);
            CodeREPL.Instance.SetPlayer(this);
            GetComponentInChildren<Renderer>().material.color = Color.blue;
            health = maxHealth;
        }
        else
        {
            GetComponentInChildren<Renderer>().material.color = Color.red;
        }
	}
	
	// Update is called once per frame
	void Update () {
        if (netview.isMine)
        {

            if (MoveQueue.Count > 0)
            {
                if (queueIndex >= MoveQueue.Count)
                    queueIndex = MoveQueue.Count - 1;

                if (MoveQueue[queueIndex].state == QueueEntry.State.DoneProcessing)
                {
                    ClearQueue();
                }
            }
            if (current != null && current.state == QueueEntry.State.Processing)
            {
                if (current.type == QueueEntry.EntryType.Move)
                {
                    transform.position = Vector3.Lerp(current.CurrentLocation, current.NextLocation, current.interPos += moveSpeed * Time.deltaTime);
                    if (current.interPos >= 1)
                    {
                        DConsole.Log(string.Format("Moved from {0} to {1}.", current.CurrentLocation, current.NextLocation));
                        current.state = QueueEntry.State.DoneProcessing;
                        MoveQueue[queueIndex] = current;
                        current = default(QueueEntry);
                        queueIndex++;
                        NextLocation();
                    }
                }
                else if (current.type == QueueEntry.EntryType.Attack)
                {
                    Ray ray = new Ray(transform.position, current.direction);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 1, mask))
                    {
                        CharacterControl _victim = hit.collider.GetComponent<CharacterControl>();
                        NetworkManager.Instance.AttackPlayer(_victim.name, -AttackAmount);
                        CodeREPL.Instance.Log("Attacked " + _victim.name + " successfully!");
                        current = default(QueueEntry);
                        queueIndex++;
                        NextLocation();
                    }
                    else
                    {
                        CodeREPL.Instance.Log("Nothing to attack...");
                        current = default(QueueEntry);
                        queueIndex++;
                        NextLocation();
                    }
                }
            }
        }
        else
        {
            syncTime += Time.deltaTime;
            transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
        }
	}

    public void AddEntry(Vector3Int direction, QueueEntry.EntryType type)
    {
        MoveQueue.Add(new QueueEntry(direction, type));
        
        if ((queueIndex == MoveQueue.Count - 1 && MoveQueue[queueIndex].state == QueueEntry.State.DoneProcessing) || queueIndex == 0)
        {
            NextLocation();
        }
    }

    public void NextLocation()
    {
        if (MoveQueue.Count > 0 && queueIndex <= MoveQueue.Count - 1 && MoveQueue[queueIndex].state == QueueEntry.State.NotProcessed)
        {
            DConsole.Log("doing movement.");
            QueueEntry tmp = MoveQueue[MoveQueue.Count - 1];
            tmp.CurrentLocation = transform.position;
            Vector3Int nextLoc = tmp.CurrentLocation + tmp.direction;;

            if (TerrainController.Instance.GetBlock(nextLoc.x, nextLoc.y, nextLoc.z) == 0)
            {
                tmp.state  = QueueEntry.State.Processing;
                tmp.NextLocation = nextLoc;
                MoveQueue[queueIndex] = tmp;
                current = tmp;
                DConsole.Log("Moving to " + nextLoc);
            }
            else
            {
                queueIndex++;
                DConsole.Log("Cannot move to " + nextLoc);
                ClearQueue();
            }
        }
    }

    [LuaFunc("Player_Int", "ClearQueue", "Clears the move queue")]
    public void ClearQueue()
    {
        MoveQueue.Clear();
        current = default(QueueEntry);
        queueIndex = 0;
    }

    [LuaFunc("Player_Int", "Move", "Move the player", "direction")]
    public void Move(int _dir)
    {
        Loom.QueueOnMainThread(() =>
        {
            Vector3Int direction = GetDirectionVector(_dir);
            AddEntry(direction, QueueEntry.EntryType.Move);
        });
    }

    [LuaFunc("Player_Int", "Set", "Set voxel location of player.", "x", "y", "z")]
    public void Set(int x, int y, int z)
    {
        Loom.QueueOnMainThread(() => 
        {
            transform.position = VoxelConversions.VoxelToWorld(x, y, z);
        });
    }

    [LuaFunc("Player_Int", "Attack", "Attack object or player", "direction")]
    public void Attack(int _dir)
    {
        Loom.QueueOnMainThread(() =>
        {
            Vector3Int direction = GetDirectionVector(_dir);
            AddEntry(direction, QueueEntry.EntryType.Attack);
        });
    }

    [LuaFunc("Player_Int", "AddHealth", "increase health by amount.", "amount")]
    public void AddHealth(float amount)
    {
        Loom.QueueOnMainThread(() => 
        {
            health += amount;
        });
    }

    [LuaFunc("Player_Int", "SetHealth", "Set health directly.", "amount")]
    public void SetHealth(float amount)
    {
        Loom.QueueOnMainThread(() =>
        {
            health = amount;
        });
    }

    [LuaFunc("Player_Int", "MaxHealth", "Set health to max.")]
    public void MaxHealth()
    {
        Loom.QueueOnMainThread(() =>
        {
            health = maxHealth;
        });
    }

    [LuaFunc("Player_Int", "SetMaxHealth", "Set Max health.", "amount")]
    public void SetMaxHealth(float amount)
    {
        Loom.QueueOnMainThread(() =>
        {
            maxHealth = amount;
        });
    }

    public void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
    {
        string _name = info.networkView.initialData.ReadString();
        gameObject.name = _name;
        NetworkManager.Instance.Clients[name].SetPlayerObject(gameObject);
    }

    public void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
    {
        Vector3 syncPos = Vector3.zero;
        if (stream.isWriting)
        {
            syncPos = transform.position;
            stream.Serialize(ref syncPos);
        }
        else
        {
            stream.Serialize(ref syncPos);
            syncTime = 0f;
            syncDelay = Time.time - lastSyncTime;
            lastSyncTime = Time.time;

            syncStartPosition = transform.position;
            syncEndPosition = syncPos;
        }
    }

    private Vector3 GetDirectionVector(int _dir)
    {
        Vector3 direction = new Vector3Int();
        switch (_dir)
        {
            case 0: // down
                direction = new Vector3Int(0, 0, -1);
                break;

            case 1: // left
                direction = new Vector3Int(-1, 0, 0);
                break;

            case 2: // up
                direction = new Vector3Int(0, 0, 1);
                break;

            case 3: // right
                direction = new Vector3Int(1, 0, 0);
                break;

            default: // none
                ConsoleWpr.LogWarning("Invalid direction.");
                break;
        }
        return direction;
    }
}
