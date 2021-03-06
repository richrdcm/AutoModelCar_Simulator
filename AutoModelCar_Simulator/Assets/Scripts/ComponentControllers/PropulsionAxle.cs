﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using autominy_msgs = RosSharp.RosBridgeClient.Messages.Autominy;

public class PropulsionAxle : MonoBehaviour
{
    public RosConnector Connection;

    public AnimationCurve speed_pwm_interp = new AnimationCurve(new Keyframe(-1000, -5), new Keyframe(0, 0), new Keyframe(1000, 5)); 
    public AnimationCurve speed_real_interp = new AnimationCurve(new Keyframe(-5, -5), new Keyframe(0, 0), new Keyframe(5, 5)); 
    public AnimationCurve speed_normalized_interp = new AnimationCurve(new Keyframe(-1, -5), new Keyframe(0, 0), new Keyframe(1, 5)); 

    public string speed_pwm_topic = "/manual_control/speed_pwm";
    public string speed_real_topic = "/manual_control/speed_real";
    public string speed_normalized_topic = "/manual_control/speed_nrm";
    public string ticks_topic = "/sensors/arduino/ticks";
    public float meters_per_tick = 0.0026f;
    public float ticks_frequency = 35f;
    public Transform left_wheel, right_wheel;
    
    // Inter-wheel distance:
    public float T {
        get {return Vector3.Distance(left_wheel.position, right_wheel.position);}
        set {Globals.Instance.DevConsole.error("Setting interwheel distance not implemented!");}
    }

    public float speed_real {
        get { return _speed_real; }
    }

    private string speed_pwm_sub, speed_real_sub, speed_normalized_sub;

    private float _speed_real=0, _speed_topic;
    private float accel_stime = 0.0f;
    private float _speed_goal = 0.0f;
    private float last_frame_real_speed = 0.0f;

    private float delta_speed = 0.0f, delta_accel = 0.0f;
    public float delta_accl_mul = 0.4f;
    public float delta_speed_damp = 0.8f;
    public float delta_speed_mul = 0.055f;
    public bool instant_response = false;
    private float dist_travelled_total = 0.0f;
    private float dist_travelled_mod = 0.0f;
    private float stime;

    private TicksPublisher ticks_publisher;


    private void read_interp(out AnimationCurve curve, string path) {
        string text;
        curve = new AnimationCurve();
		try {
			text = System.IO.File.ReadAllText(path);
		}
		catch(System.IO.FileNotFoundException e) {
			Globals.Instance.DevConsole.error(e.FileName + " could not be found!");
            return;
		}

		string[] linesInFile = text.Split('\n');
	
		foreach (string line in linesInFile)
		{
			if(line.Length<1) continue;
			string[] entry = line.Split(':');
            float t = float.Parse(entry[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            float v = float.Parse(entry[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            curve.AddKey(t, v);
		}
    }

    private void interps() {
        read_interp(out speed_normalized_interp, "UserSettings\\propulsionaxle_interp_nrm.txt");
        read_interp(out speed_pwm_interp, "UserSettings\\propulsionaxle_interp_pwm.txt");
        read_interp(out speed_real_interp, "UserSettings\\propulsionaxle_interp_real.txt");
    }

    private void naming() {
        PropComponentGroup master = GetComponentInParent(typeof(PropComponentGroup)) as PropComponentGroup; 
        if(master==null) {Globals.Instance.DevConsole.error("Component without master group encountered!"); return;}
        this.gameObject.name = master.gameObject.name + "_propaxle";
        speed_pwm_topic = Globals.Instance.normalize_from_settings("Default_TopicNames_SpeedPwm", master.id.ToString(), master.gameObject.name, "propaxle");
        speed_real_topic = Globals.Instance.normalize_from_settings("Default_TopicNames_SpeedReal", master.id.ToString(), master.gameObject.name, "propaxle");
        speed_normalized_topic = Globals.Instance.normalize_from_settings("Default_TopicNames_SpeedNormalized", master.id.ToString(), master.gameObject.name, "propaxle");
    }

    void Start()
    {
        naming();
        interps();
        if(!Connection) Connection = Globals.Instance.Connection;
        speed_pwm_sub = Connection.RosSocket.Subscribe<autominy_msgs.Autominy_SpeedPWMCommand>(speed_pwm_topic, speed_pwm_callback);
        speed_real_sub = Connection.RosSocket.Subscribe<autominy_msgs.Autominy_SpeedCommand>(speed_real_topic, speed_real_callback);
        speed_normalized_sub = Connection.RosSocket.Subscribe<autominy_msgs.Autominy_NormalizedSpeedCommand>(speed_normalized_topic, speed_normalized_callback);
        ticks_publisher = gameObject.AddComponent(typeof(TicksPublisher)) as TicksPublisher;
        ticks_publisher.Topic = ticks_topic;
        stime = Time.time;
    }

    void FixedUpdate()
    {
        if(instant_response) return;
        delta_accel = (_speed_goal - _speed_real) * delta_accl_mul;
        delta_speed += delta_accel;
        delta_speed *= delta_speed_damp;
        _speed_real += delta_speed * delta_speed_mul;
    }

    void Update() {
        if(Time.time > stime + 1.0f/ticks_frequency) {
            ticks_publisher.publish_ticks(Mathf.FloorToInt(dist_travelled_mod/meters_per_tick));
            dist_travelled_mod = dist_travelled_mod % meters_per_tick;
            stime = Time.time;
        }
    }

    private void speed_pwm_callback(autominy_msgs.Autominy_SpeedPWMCommand data) {
        _speed_goal = speed_pwm_interp.Evaluate((float)data.value);
        if(instant_response) _speed_real = _speed_goal; 
    }

    private void speed_real_callback(autominy_msgs.Autominy_SpeedCommand data) {
        _speed_goal = speed_real_interp.Evaluate((float)data.value);
        if(instant_response) _speed_real = _speed_goal; 
    }

    private void speed_normalized_callback(autominy_msgs.Autominy_NormalizedSpeedCommand data) {
        _speed_goal = speed_normalized_interp.Evaluate((float)data.value);
        if(instant_response) _speed_real = _speed_goal; 
    }

    public void set_speed(float mps) {
        _speed_goal = mps;
        if(instant_response) _speed_real = _speed_goal;
    }

    public void set_speed_override(float mps) {
        _speed_goal = mps;
        _speed_real = _speed_goal;
    }

    public void stop() {
        _speed_goal = 0.0f;
        _speed_real = 0.0f;
    }

    public void add_distance_travelled(float delta) {
        dist_travelled_total += delta;
        dist_travelled_mod += delta;
    }
}
