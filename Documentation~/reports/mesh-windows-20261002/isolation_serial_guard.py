"""Refuse changes/tests while the owned isolation project is locked or restores TAI."""
from pathlib import Path
import ctypes
project=Path(__file__).resolve().parents[1]/'NBFXMeshValidation-20261002'
def ensure_idle():
    assert project.resolve()==Path('D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002')
    lock=project/'Temp/UnityLockfile'
    if lock.exists():
        kernel=ctypes.WinDLL('kernel32',use_last_error=True)
        kernel.CreateFileW.restype=ctypes.c_void_p
        kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p]
        kernel.CloseHandle.argtypes=[ctypes.c_void_p]
        handle=kernel.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None)
        assert handle!=ctypes.c_void_p(-1).value,'Isolation Editor owns the project; inspect its live scenes before proceeding'
        kernel.CloseHandle(handle)
    scenes=project/'Library/LastSceneManagerSetup.txt'
    assert scenes.exists() and 'tai' not in scenes.read_text().lower(), 'Do not trigger TAI Domain Reload'
if __name__=='__main__':
    ensure_idle();print('Owned isolation unlocked; restored scene setup contains no TAI')
