import { useState } from 'react';


const useLocalStorage = <T>(key: string, initialValue: T) => {
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const item = window.localStorage.getItem(key);
      if (item !== null) {
        if(['{','['].includes(item[0])) {
          return JSON.parse(item);
        }
        if (['true','false'].includes(item)) {
          return item === 'true';
        }
        return item;
      }
      return initialValue;
    } catch (error) {
      console.error(error);
      return initialValue;
    }
  });

  const setValue = (value: T | ((val: T) => T)) => {
    try {
      const valueToStore = value instanceof Function ? value(storedValue) : value;
      setStoredValue(valueToStore);
      let vToStore: string = '';
      if(['{','['].includes(vToStore[0])) {
        vToStore = JSON.stringify(vToStore);
      } else if (vToStore.startsWith('"')) {
        vToStore = vToStore.substring(1);
      } else if (vToStore.endsWith('"')) {
        vToStore = vToStore.substring(0, vToStore.length -1);
      } else {
        vToStore = `${valueToStore}`;
      }
      window.localStorage.setItem(key, vToStore);
      window.dispatchEvent(
        new CustomEvent('localStorageUpdated', {
          detail: { key, value: valueToStore },
        })
      );
    } catch (error) {
      console.error(error);
    }
  };

  const removeValue = () => {
    try {
      window.localStorage.removeItem(key);
    } catch (error) {
      console.error(error);
    }
  }

  return [storedValue, setValue, removeValue] as const;
}

export default useLocalStorage;
