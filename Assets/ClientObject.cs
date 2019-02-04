using System.Collections.Concurrent;
using System.Threading;
using NetMQ;
using UnityEngine;
using NetMQ.Sockets;

public class NetMqListener
{
    private readonly Thread _listenerWorker;

    private bool _listenerCancelled;

    public delegate void MessageDelegate(string message);

    private readonly MessageDelegate _messageDelegate;

    private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

    private void ListenerWork()
    {
        AsyncIO.ForceDotNet.Force();
        using (var subSocket = new SubscriberSocket())
        {
            subSocket.Options.ReceiveHighWatermark = 1000;
            subSocket.Connect("tcp://localhost:12345");
            subSocket.Subscribe("");
            while (!_listenerCancelled)
            {
                string frameString;
                if (!subSocket.TryReceiveFrameString(out frameString)) continue;
                Debug.Log(frameString);
                _messageQueue.Enqueue(frameString);
            }
            subSocket.Close();
        }
        NetMQConfig.Cleanup();
    }

    public void Update()
    {
        while (!_messageQueue.IsEmpty)
        {
            string message;
            if (_messageQueue.TryDequeue(out message))
            {
                _messageDelegate(message);
            }
            else
            {
                break;
            }
        }
    }

    public NetMqListener(MessageDelegate messageDelegate)
    {
        _messageDelegate = messageDelegate;
        _listenerWorker = new Thread(ListenerWork);
    }

    public void Start()
    {
        _listenerCancelled = false;
        _listenerWorker.Start();
    }

    public void Stop()
    {
        _listenerCancelled = true;
        _listenerWorker.Join();
    }
}

public class ClientObject : MonoBehaviour
{
    private NetMqListener _netMqListener;
    [SerializeField]
    protected GameObject head;
    [SerializeField]    
    protected GameObject neck;

    private LineRenderer head_neck;

    private void HandleMessage(string message)
    {
        var splittedStrings = message.Split(' ');
        if (splittedStrings.Length != 3) return;
        var x = float.Parse(splittedStrings[0]);
        var y = float.Parse(splittedStrings[1]);
        var z = float.Parse(splittedStrings[2]);
        //transform.position = new Vector3(x, y, z);
        
        setJoints(x,y,z);
        drawLines();
    }
    private void setJoints(float x, float y,float z)
    {
        head.transform.position = new Vector3(x+0,y+0,z+0);
        neck.transform.position = new Vector3(x+1,y+1,z+1);
    }
    private void initializeLines()
    {
        //LineRenderer head_neck = gameObject.AddComponent<LineRenderer>();
        head_neck = new LineRenderer();
        /**
        LineRenderer neck_r_shoulder = gameObject.AddComponent<LineRenderer>();
        LineRenderer neck_l_shoulder = gameObject.AddComponent<LineRenderer>();
        LineRenderer r_shoulder_r_elbow = gameObject.AddComponent<LineRenderer>();
        LineRenderer l_shoulder_l_elbow = gameObject.AddComponent<LineRenderer>();
        LineRenderer r_elbow_r_wrist = gameObject.AddComponent<LineRenderer>();
        LineRenderer l_elbow_l_wrist= gameObject.AddComponent<LineRenderer>();
        LineRenderer neck_pelvis = gameObject.AddComponent<LineRenderer>();
        LineRenderer pelvis_r_hip = gameObject.AddComponent<LineRenderer>();
        LineRenderer pelvis_l_hip = gameObject.AddComponent<LineRenderer>();
        LineRenderer r_hip_r_knee = gameObject.AddComponent<LineRenderer>();
        LineRenderer l_hip_l_knee= gameObject.AddComponent<LineRenderer>();
        LineRenderer r_knee_r_ankle = gameObject.AddComponent<LineRenderer>();
        LineRenderer l_knee_l_ankle= gameObject.AddComponent<LineRenderer>();
         */

    }
    private void drawLines()
    {
        head_neck.SetPosition(0, head.transform.position);
        head_neck.SetPosition(1, neck.transform.position);

    }

    private void Start()
    {
        _netMqListener = new NetMqListener(HandleMessage);
        _netMqListener.Start();
        
    }

    private void Update()
    {
        initializeLines();
        _netMqListener.Update();
    }

    private void OnDestroy()
    {
        _netMqListener.Stop();
    }
}
