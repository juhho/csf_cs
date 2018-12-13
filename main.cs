using System;
using System.Runtime.InteropServices;

namespace csc
{
public class AimAssist {
    private static Player target = new Player(0);
    private static Convar sensitivity;
    private static Convar mp_teammates_are_enemies;
    private static int    current_tick, previous_tick;
    private static float  flsensitivity;

    public static void Main()
    {
        if (!engine.initialize()) {
            return;
        }
        sensitivity = Convar.find(crc32.initialize(-889421740, 11, 1387158398));
        mp_teammates_are_enemies = Convar.find(crc32.initialize(1235347760, 24, 304350720));
        while (engine.isRunning()) {
            if (engine.isInGame()) {
                aim();
            }
        }
    }

    static void aim()
    {
        Player  self;
        float[] vangle;

        self          = entity.getClientEntity(engine.getLocalPlayer());
        vangle        = engine.getViewAngles();
        current_tick  = self.getTickCount();
        flsensitivity = self.isScoped() ?
            (self.getFov() / 90.0f) * sensitivity.getFloat() :
            sensitivity.getFloat();
        if (InputSystem.isButtonDown(107) == 1 || InputSystem.isButtonDown(111) == 1) {
            if (!target.isValid() && !getTarget(self, vangle))
                return;
            aimAtTarget(vangle, getTargetAngle(self, target, 8), 0.0111111111111111f, 5f);
        } else {
            target.self = 0;
        }
    }

    static void aimAtTarget(float[] vangle, float[] angle, float fov, float smooth)
    {
        float x, y, sx, sy;


        y = vangle[0] - angle[0]; x = vangle[1] - angle[1];
        if (y > 89.0f)   y = 89.0f;   else if (y < -89.0f)  y = -89.0f;
        if (x > 180.0f)  x -= 360.0f; else if (x < -180.0f) x += 360.0f;

        if (fabs(x) / 180.0f >= fov)
            return;
        if (fabs(y) / 89.0f >= fov)
            return;
        x = ((x / flsensitivity) / 0.022f);
        y = ((y / flsensitivity) / -0.022f);
        if (smooth != 0) {
            sx = 0.0f; sy = 0.0f;
            if (sx < x) {
                sx += 1.0f + (x / smooth);
            } else if (sx > x) {
                sx -= 1.0f - (x / smooth);
            }
            if (sy < y) {
                sy += 1.0f + (y / smooth);
            } else if (sy > y) {
                sy -= 1.0f - (y / smooth);
            }
        } else {
            sx = x; sy = y;
        }
        if ( current_tick - previous_tick > 0 ) {
            previous_tick = current_tick;
            mouse_event(0x0001, (int)sx, (int)sy, 0, 0);
        }
    }

    static bool getTarget(Player self, float[] vangle)
    {
        float     best_fov;
        int       i;
        Player    e;
        float     fov;

        best_fov = 9999f;
        for (i = 1; i < engine.getMaxClients(); i++) {
            e = entity.getClientEntity(i);
            if (!e.isValid())
                continue;
            if (mp_teammates_are_enemies.getInt() == 0 && self.getTeam() == e.getTeam())
                continue;
            fov = (float)angleBetween(vangle, getTargetAngle(self, e, 8));
            if (fov < best_fov) {
                best_fov = fov;
                target   = e;
            }
        }
        return best_fov != 9999.0f;
    }
    static void sincos(float radians, ref float sine, ref float cosine)
    {
        sine = (float)sin(radians);
        cosine = (float)cos(radians);
    }
    static void angleVector(float[] angle, ref float[] forward)
    {
        float sp = 0, sy = 0, cp = 0, cy = 0;

        sincos((float)(angle[0]) * (float)(Math.PI / 180f), ref sp, ref cp);
        sincos((float)(angle[1]) * (float)(Math.PI / 180f), ref sy, ref cy);
        forward[0] = cp * cy;
        forward[1] = cp * sy;
        forward[2] = -sp;
    }
    static double dot(float[] v0, float[] v1)
    {
        return (v0[0] * v1[0] + v0[1] * v1[1] + v0[2] * v1[2]);
    }
    static float length(float[] v0)
    {
        return (v0[0] * v0[0] + v0[1] * v0[1] + v0[2] * v0[2]);
    }
    private static double angleBetween(float[] p0, float[] p1)
    {
        float[] a0 = new float[3], a1 = new float[3];
        angleVector(p0, ref a0);
        angleVector(p1, ref a1);
        return Math.Acos(dot(a0, a1) / length(a0)) * (float)(180f / Math.PI);
    }
    private static void angleNormalize(ref float[] angle)
    {
        float radius = 1f / (float)(sqrt(angle[0]*angle[0] + angle[1]*angle[1] + angle[2]*angle[2]) + 1.192092896e-07f);
        angle[0] *= radius; angle[1] *= radius; angle[2] *= radius;
    }
    static float[] getTargetAngle(Player self, Player target, int index)
    {
        float[]     m = target.getBonePos(index);
        float[]     c, p;

        c    = self.getVecOrigin();
        p    = self.getVecView();
        c[0] += p[0]; c[1] += p[1]; c[2] += p[2];
        m[0] -= c[0]; m[1] -= c[1]; m[2] -= c[2];
        angleNormalize(ref m);
        vectorAngles(m, ref m);
        if (self.getShotsFired() > 1) {
            p = self.getVecPunch();
            m[0] -= p[0] * 2f; m[1] -= p[1] * 2f; m[2] -= p[2] * 2f;
        }
        vectorClamp(ref m);
        return m;
    }
    static void vectorClamp(ref float[] v)
    {
        if (v[0] > 89.0f && v[0] <= 180.0f)
            v[0] = 89.0f;
        else if (v[0] > 180.0f)
            v[0] = v[0] - 360.0f;
        else if (v[0] < -89.0f)
            v[0] = -89.0f;
        if (v[1] > 180.0f)
            v[1] -= 360.0f;
        else if (v[1] < -180.0f)
            v[1] += 360.0f;
        v[2] = 0;
    }
    static void vectorAngles(float[] forward, ref float[] angles)
    {
        float tmp, yaw, pitch;

        if (forward[1] == 0f && forward[0] == 0f) {
            yaw = 0;
            if (forward[2] > 0f) {
                pitch = 270f;
            } else {
                pitch = 90f;
            }
        } else {
            yaw = (float)(atan2(forward[1], forward[0]) * 180f / 3.14159265358979323846f);
            if (yaw < 0) {
                yaw += 360f;
            }
            tmp = (float)sqrt(forward[0] * forward[0] + forward[1] * forward[1]);
            pitch = (float)(atan2(-forward[2], tmp) * 180 / 3.14159265358979323846f);
            if (pitch < 0) {
                pitch += 360f;
            }
        }
        angles[0] = pitch;
        angles[1] = yaw;
        angles[2] = 0f;
    }
    [DllImport("ntdll")] public static extern double atan2(double x, double y);
    [DllImport("ntdll")] public static extern double sqrt(double p0);
    [DllImport("ntdll")] public static extern double fabs(double p0);
    [DllImport("ntdll")] public static extern double sin(double p0);
    [DllImport("ntdll")] public static extern double cos(double p0);
    [DllImport("user32")] public static extern int mouse_event(int p0, int p1, int p2, int p3, long p4);
}

class engine {
    public static Process       e_mem;
    public static VirtualTables e_vt;
    public static Netvars       e_off;

    public static bool    isRunning()                { return e_mem.exists(); }
    public static int     getLocalPlayer()           { return e_mem.read<int>(e_off.clientState + e_off.c_getLocalPlayer); }
    public static float[] getViewAngles()            { return e_mem.read_array<float>(e_off.clientState + e_off.c_getViewAngles, 3); }
    public static int     getMaxClients()            { return e_mem.read<int>(e_off.clientState + e_off.c_getMaxClients);  }
    public static bool    isInGame()                 { return e_mem.read<int>(e_off.clientState + e_off.c_getState) == 6; }

    static public bool initialize()
    {
        if ((e_mem = Process.attach(crc32.initialize(0x3B910704, 8, 0x7106C4BC))) == null) {
            Console.WriteLine("[!]process is not running");
            return false;
        }
        if ((e_vt = VirtualTables.initialize()) == null) {
            Console.WriteLine("[!]could not initialize virtualtables");
            return false;
        }
        if ((e_off = Netvars.initialize()) == null) {
            Console.WriteLine("[!]could not initialize netvars");
            return false;
        }
        Console.WriteLine(
            @"[*]vtables:
                VClient:                         0x{0:X}
                VClientEntityList:               0x{1:X}
                VEngineClient:                   0x{2:X}
                VEngineCvar:                     0x{3:X}
                InputSystemVersion:              0x{4:X}
                [*]offsets:
                entityList:                      0x{5:X}
                clientState:                     0x{6:X}
                c_getLocalPlayer:                0x{7:X}
                c_getViewAngles:                 0x{8:X}
                c_getMaxClients:                 0x{9:X}
                c_getState:                      0x{10:X}
                i_button:                        0x{11:X}
                i_analog:                        0x{12:X}
                i_analogDelta:                   0x{13:X}
                [*]netvars:
                DT_BasePlayer:                   m_iHealth:           0x{14:X}
                DT_BasePlayer:                   m_vecViewOffset:     0x{15:X}
                DT_BasePlayer:                   m_lifeState:         0x{16:X}
                DT_BasePlayer:                   m_nTickBase:         0x{17:X}
                DT_BasePlayer:                   m_vecVelocity:       0x{18:X}
                DT_BasePlayer:                   m_vecPunch:          0x{19:X}
                DT_BasePlayer:                   m_iFOV:              0x{20:X}
                DT_BaseEntity:                   m_iTeamNum:          0x{21:X}
                DT_BaseEntity:                   m_vecOrigin:         0x{22:X}
                DT_CSPlayer:                     m_hActiveWeapon:     0x{23:X}
                DT_CSPlayer:                     m_iShotsFired:       0x{24:X}
                DT_CSPlayer:                     m_bIsScoped:         0x{25:X}
                DT_BaseAnimating:                m_dwBoneMatrix:      0x{26:X}
            [*]convar demo
                sensitivity:                     {27}
                volume:                          {28}
                cl_crosshairsize:                {29}
            [*]sdk info:
                creator:                         ekknod
                repo:                            github.com/ekknod/csf",
                engine.e_vt.client.address, engine.e_vt.entity.address, engine.e_vt.engine.address, engine.e_vt.cvar.address,
                engine.e_vt.inputsystem.address, engine.e_off.entityList, engine.e_off.clientState, engine.e_off.c_getLocalPlayer,
                engine.e_off.c_getViewAngles, engine.e_off.c_getMaxClients, engine.e_off.c_getMaxClients, engine.e_off.i_button,
                engine.e_off.i_analog, engine.e_off.i_analogDelta, engine.e_off.e_iHealth, engine.e_off.e_vecViewOffset,
                engine.e_off.e_lifeState, engine.e_off.e_nTickBase, engine.e_off.e_vecVelocity, engine.e_off.e_vecPunch,
                engine.e_off.e_iFOV, engine.e_off.e_iTeamNum, engine.e_off.e_vecOrigin, engine.e_off.e_hActiveWeapon,
                engine.e_off.e_iShotsFired, engine.e_off.e_bIsScoped, engine.e_off.e_dwBoneMatrix,
                Convar.find(crc32.initialize(-889421740, 11, 1387158398)).getFloat(),
                Convar.find(crc32.initialize(938228256, 6, 2002548591)).getFloat(),
                Convar.find(crc32.initialize(143337850, 16, 1131805743)).getInt()
            ) ;
        return true;
    }
}

class entity { public static Player getClientEntity(int index) { return new Player(engine.e_mem.read<int>(engine.e_off.entityList + index * 0x10)); } }

class InputSystem {
    public static int isButtonDown(int button) { return (engine.e_mem.read<int>(engine.e_vt.inputsystem.address + (((button >> 5 ) * 4) + engine.e_off.i_button)) >> (button & 31)) & 1; }
    public static int[] getMouseAnalog() { return engine.e_mem.read_array<int>(engine.e_vt.inputsystem.address + engine.e_off.i_analog, 2); }
    public static int[] getMouseAnalogDelta() { return engine.e_mem.read_array<int>(engine.e_vt.inputsystem.address + engine.e_off.i_analogDelta, 2); }
}

class Player {
    public int self;
    
    public         Player(int self)  { this.self = self; }
    public bool    isScoped()        { return engine.e_mem.read<bool>(self + engine.e_off.e_bIsScoped); }
    public int     getTeam()         { return engine.e_mem.read<int>(self + engine.e_off.e_iTeamNum); }
    public int     getHealth()       { return engine.e_mem.read<int>(self + engine.e_off.e_iHealth); }
    public int     getLifeState()    { return engine.e_mem.read<int>(self + engine.e_off.e_lifeState); }
    public int     getTickCount()    { return engine.e_mem.read<int>(self + engine.e_off.e_nTickBase); }
    public int     getShotsFired()   { return engine.e_mem.read<int>(self + engine.e_off.e_iShotsFired); }
    public int     getFov()          { return engine.e_mem.read<int>(self + engine.e_off.e_iFOV); }
    private int    getActiveWeapon() { return engine.e_mem.read<int>(self + engine.e_off.e_hActiveWeapon); }
    public int     getWeapon()       { return engine.e_mem.read<int>(engine.e_off.entityList + ((getActiveWeapon() & 0xFFF) - 1) * 0x10); }
    private int    getBoneMatrix()   { return engine.e_mem.read<int>(self + engine.e_off.e_dwBoneMatrix); }
    public float[] getVecOrigin()    { return engine.e_mem.read_array<float>(self + engine.e_off.e_vecOrigin, 3); }
    public float[] getVecView()      { return engine.e_mem.read_array<float>(self + engine.e_off.e_vecViewOffset, 3); }
    public float[] getVecVelocity()  { return engine.e_mem.read_array<float>(self + engine.e_off.e_vecVelocity, 3); }
    public float[] getVecPunch()     { return engine.e_mem.read_array<float>(self + engine.e_off.e_vecPunch, 3); }
    public float[] getEyePos()
    {
        float[] v, o;
        v = getVecView();
        o = getVecOrigin();
        return new float[] { v[0] + o[0], v[1] + o[1], v[2] + o[2] };
    }
    public float[] getBonePos(int i)
    {
        return new float[] {
            engine.e_mem.read<float>(getBoneMatrix() + 0x30 * i + 0x0C),
            engine.e_mem.read<float>(getBoneMatrix() + 0x30 * i + 0x1C),
            engine.e_mem.read<float>(getBoneMatrix() + 0x30 * i + 0x2C)
        } ;
    }
    public bool isValid()
    {
        int health = getHealth();
        return self != 0 && getLifeState() == 0 && health > 0 && health < 1337;
    }
}

class Convar
{
    private int self;
    private Convar(int self) { this.self = self; }
    public char[] getName() { return engine.e_mem.read_array<char>(engine.e_mem.read<int>(self + 0xC), 120); }
    public char[] getString() { return engine.e_mem.read_array<char>(engine.e_mem.read<int>(self + 0x24), 120); }
    public int getInt() { return engine.e_mem.read<int>(self + 0x30) ^ self; }
    public float getFloat() { return BitConverter.ToSingle(BitConverter.GetBytes(engine.e_mem.read<int>(self + 0x2C) ^ self), 0); }
    public static Convar find(crc32 crc)
    {
        var a1  = engine.e_mem.read<int>(engine.e_mem.read<int>(engine.e_vt.cvar.address + 0x34));
        while ((a1 = engine.e_mem.read<int>(a1 + 0x4)) != 0)
            if (crc.compare<char>(engine.e_mem.read_array<char>(engine.e_mem.read<int>(a1 + 0xc), 120)))
                return new Convar(a1);
        return null;
    }
    public static Convar find(string name)
    {
        return find(crc32.str_initialize(name));
    }
}

class Netvars
{
    public int entityList,       clientState;
    public int c_getLocalPlayer, c_getViewAngles, c_getMaxClients,  c_getState;
    public int i_button,         i_analog,        i_analogDelta,    e_iHealth;
    public int e_vecViewOffset,  e_lifeState,     e_nTickBase,      e_vecVelocity;
    public int e_vecPunch,       e_iFOV,          e_iTeamNum,       e_vecOrigin;
    public int e_hActiveWeapon,  e_iShotsFired,   e_bIsScoped,      e_dwBoneMatrix;

    public static Netvars initialize()
    {
        var         nv = new Netvars();
        NetvarTable t;

        nv.entityList       = engine.e_vt.entity.address - (engine.e_mem.read<int>(engine.e_vt.entity.function(5) + 0x22) - 0x38);
        nv.clientState      = engine.e_mem.read<int>(engine.e_mem.read<int>(engine.e_vt.engine.function(18) + 0x16));
        nv.c_getLocalPlayer = engine.e_mem.read<int>(engine.e_vt.engine.function(12) + 0x16);
        nv.c_getViewAngles  = engine.e_mem.read<int>(engine.e_vt.engine.function(19) + 0xB2);
        nv.c_getMaxClients  = engine.e_mem.read<int>(engine.e_vt.engine.function(20) + 0x07);
        nv.c_getState       = engine.e_mem.read<int>(engine.e_vt.engine.function(26) + 0x07);
        nv.i_button         = engine.e_mem.read<int>(engine.e_vt.inputsystem.function(15) + 0x21D);
        nv.i_analog         = engine.e_mem.read<int>(engine.e_vt.inputsystem.function(18) + 0x09);
        nv.i_analogDelta    = engine.e_mem.read<int>(engine.e_vt.inputsystem.function(18) + 0x29);
        try {
            t = NetvarTable.open(crc32.initialize(-518714413, 13, 527684971));                       /* DT_BasePlayer */
                nv.e_iHealth       = t.getOffset(crc32.initialize(1633193003, 9, 1145823205));       /* m_iHealth */
                nv.e_vecViewOffset = t.getOffset(crc32.initialize(1820487808, 18, 1153529757));      /* m_vecViewOffset[0] */
                nv.e_lifeState     = t.getOffset(crc32.initialize(-274821372, 11, 746473911));       /* m_lifeState */
                nv.e_nTickBase     = t.getOffset(crc32.initialize(-1409136347, 11, 1142360283));     /* m_nTickBase */
                nv.e_vecVelocity   = t.getOffset(crc32.initialize(1830428536, 16, 1177502929));      /* m_vecVelocity[0] */
                nv.e_vecPunch      = t.getOffset(crc32.initialize(1341626896, 7, 228412478)) + 0x70; /* m_Local */
                nv.e_iFOV          = t.getOffset(crc32.initialize(1362244146, 6, 93949321));         /* m_iFOV */
            t = NetvarTable.open(crc32.initialize(66898466, 13, 949955796));                         /* DT_BaseEntity */
                nv.e_iTeamNum      = t.getOffset(crc32.initialize(-670822643, 10, 1040583495));      /* m_iTeamNum */
                nv.e_vecOrigin     = t.getOffset(crc32.initialize(1857655783, 11, 883947371));       /* m_vecOrigin */
            t = NetvarTable.open(crc32.initialize(-1331763933, 11, 866751482));                      /* DT_CSPlayer */
                nv.e_hActiveWeapon = t.getOffset(crc32.initialize(96205189, 15, 125279536));         /* m_hActiveWeapon */
                nv.e_iShotsFired   = t.getOffset(crc32.initialize(1111863503, 13, 1803608319));      /* m_iShotsFired */
                nv.e_bIsScoped     = t.getOffset(crc32.initialize(-2113269303, 11, 991466299));      /* m_bIsScoped */
            t = NetvarTable.open(crc32.initialize(243038784, 16, 895501995));                        /* DT_BaseAnimating */
                nv.e_dwBoneMatrix = t.getOffset(crc32.initialize(-960476223, 12, 235609196)) + 0x1C; /* m_nForceBone */
        } catch {
            return null;
        }
        return nv;
    }
}

class VirtualTables
{
    public VirtualTable client;
    public VirtualTable entity;
    public VirtualTable engine;
    public VirtualTable cvar;
    public VirtualTable inputsystem;

    public static VirtualTables initialize()
    {
        VirtualTables  vt = new VirtualTables();
        InterfaceTable t;
        
        try {
            t = InterfaceTable.open(crc32.initialize(322218740, 19, 52673840));                 /* client_panorama.dll */
                vt.client      = t.getInterface(crc32.initialize(-2117240988, 7, 517220328));   /* VClient  */
                vt.entity      = t.getInterface(crc32.initialize(988898746, 17, 584443012));    /* VClientEntityList */
            t = InterfaceTable.open(crc32.initialize(1830937564, 10, 164455238));               /* engine.dll */
                vt.engine      = t.getInterface(crc32.initialize(1246076804, 13, 1237105580));  /* VEngineClient */
            t = InterfaceTable.open(crc32.initialize(-1266026105, 11, 1598327702));             /* vstdlib.dll */
                vt.cvar        = t.getInterface(crc32.initialize(-1014352715, 11, 1570200836)); /* VEngineCvar */
            t = InterfaceTable.open(crc32.initialize(-288952189, 15, 1365201083));              /* inputsystem.dll */
                vt.inputsystem = t.getInterface(crc32.initialize(-1692259331, 18, 1628494329)); /* InputSystemVersion */
        } catch {
            return null;
        }
        return vt;
    }
}

class Process
{
    private readonly long handle;
    private readonly bool wow64;
    private readonly long peb;

    private Process(long handle, bool wow64, long teb)
    {
        this.handle  = handle;
        this.wow64   = wow64;
        this.peb     = wow64 ?
            u.read<long>(handle, teb + 0x2030, 4) :
            u.read<long>(handle, teb + 0x0060, 8) ;
    }
    ~Process() { if (this.handle != 0) NtClose(this.handle); }
    public bool exists() { int code; GetExitCodeProcess(handle, out code); return code == 0x00000103; }
    
    public long getModule(crc32 crc)
    {
        int[] a0 = wow64 ?
            new int[5] { 0x04, 0x0C, 0x14, 0x28, 0x10 } :
            new int[5] { 0x08, 0x18, 0x20, 0x50, 0x20 } ;
        long  a1 = read<long>(read<long>(this.peb + a0[1], a0[0]) + a0[2], a0[0]);
        long  a2 = read<long>(a1 + a0[0], a0[0]);
        
        while (a1 != a2) {
            if (crc.compare(read_array<short>(read<long>(a1 + a0[3], a0[0]), 120)))
                return read<long>(a1 + a0[4], a0[0]);
            a1 = read<long>(a1, a0[0]);
        }
        return 0;
    }
    public long getModule(string name) { return getModule(crc32.wcs_initialize(name)); }
    public long getExport(long module, crc32 crc)
    {
        
        long  a0 = read<int>(module + read<short>(module + 0x3C) + (this.wow64 ? 0x78 : 0x88)) + module;
        int[] a1 = read_array<int>(a0 + 0x18, 4);
 
        while (a1[0]-- > 0)
            if (crc.compare(read_array<char>(module + read<int>(module + a1[2] + (a1[0] * 4)), 120)))
                return (module + read<int>(module + a1[1] + (read<short>(module + a1[3] + (a1[0] * 2)) * 4) ));
        return 0;
    }
    public long getExport(long module, string name) { return getExport(module, crc32.str_initialize(name)); }
    private class u {
    public static T read<T>(long handle, long address, long size)
    {
        T[] v = new T[1];
        NtReadVirtualMemory(handle, address, v, size, 0);
        return v[0];
    }
    public static T[] read_array<T>(long handle, long address, long count)
    {
        T[] v = new T[count];
        NtReadVirtualMemory(handle, address, v, count * Marshal.SizeOf<T>(), 0);
        return v;
    }}
    public T[] read_array<T>(long address, long size) { return u.read_array<T>(this.handle, address, size); }
    public T read<T>(long address, long size)         { return u.read<T>(this.handle, address, size); }
    public T read<T>(long address)                    { return u.read<T>(this.handle, address, Marshal.SizeOf<T>()); }
    public static Process attach(crc32 crc)
    {
        Process process = null;
        long    handle;
        for (var list = ProcessList.first; list != null; list = list.next) {
            handle = OpenProcess(0x1000 | 0x0010, 0, list.pid);
            if (crc.compare<short>(list.name))
                process = new Process(handle, list.wow64, list.teb);
        }
        return process;
    }
    public static Process attach(string name)
    {
        return attach(crc32.wcs_initialize(name));
    }
    [DllImport("kernel32")] public static extern long OpenProcess(uint p0, uint p1, int p2);
    [DllImport("kernel32")] public static extern int GetExitCodeProcess(long p0, out int p1);
    [DllImport("ntdll")] public static extern int NtReadVirtualMemory(long p0, long p1, [Out, MarshalAs(UnmanagedType.AsAny)] object p2, long p3, long p4);
    [DllImport("ntdll")] public static extern int NtClose(long p0);
}

class ProcessList
{
    private byte[]    snap;
    private int       pos;

    private ProcessList()
    {
        uint len;
        if ((uint)NtQuerySystemInformation(57, new byte[8], 0x188, out len)                 != 0xC0000004
            || NtQuerySystemInformation(57, snap = new byte[len+=8192], len, out len)       != 0)
            throw new Exception( "[!]NtQuerySystemInformation");
        this.pos  = 0;
    }
    private T copy<T>(byte[] buffer, int offset) { T[] t = new T[1]; Buffer.BlockCopy(buffer, offset, t, 0, Marshal.SizeOf<T>()); return t[0]; }
    public bool wow64                 { get { return start <= 0xffffffffU; } }
    public int  pid                   { get { return copy<int>(snap,  pos + 0x128); } }
    public long start                 { get { return copy<long>(snap, pos + 0x160); } }
    public long teb                   { get { return copy<long>(snap, pos + 0x168); } }
    public short[] name               { get {
        var mem = new short[120];
        memcpy(mem, copy<long>(snap, pos + 0x40), 240);
        return mem;
    } }
    public ProcessList next           { get {
        if (copy<int>(snap, pos) != 0) {
            pos = copy<int>(snap, pos) + pos;
            return this;
        }
        return null;
    } }
    public static ProcessList first   { get { return new ProcessList().next; } }
    [DllImport("ntdll")] private static extern int  NtQuerySystemInformation(uint p0, byte[] p1, uint p2, out uint p3);
    [DllImport("ntdll")] private static extern long memcpy(short[] p0, long p1, long p2);
}

class VirtualTable
{
    private int self;
    public VirtualTable(int self)  { this.self = self; }
    public int address      { get  { return this.self; } }
    public int function(int index) { return engine.e_mem.read<int>(engine.e_mem.read<int>(self) + index * 4); }
}

class InterfaceTable
{
    private int self;
    private InterfaceTable(int self) { this.self = self; }
    public VirtualTable getInterface(crc32 crc)
    {
        var a0 = self;
        do {
            if (crc.compare(engine.e_mem.read_array<char>(engine.e_mem.read<int>(a0 + 0x4), 120), 4)) {
                return new VirtualTable(engine.e_mem.read<int>(engine.e_mem.read<int>(a0) + 1));
            }
        } while ((a0 = engine.e_mem.read<int>(a0 + 0x8)) != 0);
        throw new Exception("VirtualTable::getInterface");
    }
    public VirtualTable getInterface(string name)
    {
        return getInterface(crc32.str_initialize(name));
    }
    public static InterfaceTable open(crc32 crc)
    {
        var v = engine.e_mem.getExport(engine.e_mem.getModule(crc), crc32.initialize(0x1617BEAF, 15, 0x371FA0));
        return v != 0 ? new InterfaceTable(engine.e_mem.read<int>(engine.e_mem.read<int>(v - 0x6A))) : null;
    }
    public static InterfaceTable open(string dll_name)
    {
        return open(crc32.wcs_initialize(dll_name));
    }
}

class NetvarTable {
    private int self;
    public NetvarTable(int self)      { this.self = self; }
    public int getOffset(string name) { return getOffset(self, crc32.str_initialize(name)); }
    public int getOffset(crc32 crc)   { return getOffset(self, crc); }
    private int getOffset(int address, crc32 crc)
    {
        int a0 = 0, a1, a2, a3, a4, a5;

        for (a1 = 0; a1 < engine.e_mem.read<int>(address + 0x4); a1++) {
            a2 = a1 * 60 + engine.e_mem.read<int>(address);
            a3 = engine.e_mem.read<int>(a2 + 0x2C);
            if ((a4 = engine.e_mem.read<int>(a2 + 0x28)) != 0 && engine.e_mem.read<int>(a4 + 0x4) != 0)
                if ((a5 = getOffset(a4, crc)) != 0)
                    a0 += a3 + a5;
            if (crc.compare<char>(engine.e_mem.read_array<char>(engine.e_mem.read<int>(a2), 120)))
                return a3 + a0;
        }
        return a0;
    }
    public static NetvarTable open(crc32 crc)
    {
        int a0, a1;

        a0 = engine.e_mem.read<int>(engine.e_mem.read<int>(engine.e_vt.client.function(8) + 1));
        do {
            a1 = engine.e_mem.read<int>(a0 + 0xC);
            if (crc.compare(engine.e_mem.read_array<char>(engine.e_mem.read<int>(a1 + 0xC), 120), 1))
                return new NetvarTable(a1);
        } while((a0 = engine.e_mem.read<int>(a0 + 0x10)) != 0);
        throw new Exception("NetvarTable::open");
    }
    public static NetvarTable open(string name)
    {
        return open(crc32.str_initialize(name));
    }
}

class crc32
{
    public readonly int c, l, i;
    crc32(int crc, int length, int initial) { c = crc; l = length; i = initial; }
    public static crc32 initialize(int crc, int length, int initial) { return new crc32(crc, length, initial); }
    public static crc32 initialize<T>(T[] name)
    {
        var initial = new Random(Guid.NewGuid().GetHashCode()).Next();
        var length  = name.Length;
        var crc     = RtlCrc32(name, length, initial);
        return new crc32(crc, length, initial);
    }
    public static crc32 wcs_initialize(string name) { return initialize(strwcs(name)); }
    public static crc32 str_initialize(string name) { return initialize(name.ToCharArray()); }
    private static short[] strwcs(string n)
    {
        int a0 = n.Length; short[] a2 = new short[a0];
        while (--a0 > -1 && (a2[a0] = (short)(char)n[a0]) != (short)0);
        return a2;
    }
    public bool compare<T>(T[] v)                 { return RtlCrc32(v, l, i) == c; }
    public bool compare(char[] v, int extra_len)  { return compare(v) && strlen(v) - l <= extra_len; }
    public bool compare(short[] v, int extra_len) { return compare(v) && wcslen(v) - l <= extra_len; }
    [DllImport("ntdll")] private static extern int RtlCrc32([In, MarshalAs(UnmanagedType.AsAny)] object p0, long p1, int p2 );
    [DllImport("ntdll")] private static extern int strlen(char[] target);
    [DllImport("ntdll")] private static extern int wcslen(short[] target);
}

}
