import { useState, useEffect, useCallback } from 'react';
import {
  onAuthStateChanged,
  signInWithPopup,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  signOut as firebaseSignOut,
  updateProfile,
} from 'firebase/auth';
import type { User } from 'firebase/auth';
import { auth, googleProvider } from '../config/firebase';

export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, (firebaseUser) => {
      setUser(firebaseUser);
      setLoading(false);

      if (firebaseUser) {
        // Build a stub JWT for the streaks API (it only decodes, no signature check)
        const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
        const payload = btoa(JSON.stringify({ playerId: firebaseUser.uid }));
        const token = `${header}.${payload}.firebase`;

        localStorage.setItem('token', token);
        localStorage.setItem('playerId', firebaseUser.uid);
        localStorage.setItem('displayName', firebaseUser.displayName || firebaseUser.email || '');
      } else {
        localStorage.removeItem('token');
        localStorage.removeItem('playerId');
        localStorage.removeItem('displayName');
      }
    });
    return unsubscribe;
  }, []);

  const signInWithGoogle = useCallback(async () => {
    await signInWithPopup(auth, googleProvider);
  }, []);

  const signInWithEmail = useCallback(async (email: string, password: string) => {
    await signInWithEmailAndPassword(auth, email, password);
  }, []);

  const signUpWithEmail = useCallback(async (email: string, password: string, displayName: string) => {
    const cred = await createUserWithEmailAndPassword(auth, email, password);
    if (displayName) {
      await updateProfile(cred.user, { displayName });
    }
  }, []);

  const signOut = useCallback(async () => {
    await firebaseSignOut(auth);
  }, []);

  return { user, loading, signInWithGoogle, signInWithEmail, signUpWithEmail, signOut };
}
