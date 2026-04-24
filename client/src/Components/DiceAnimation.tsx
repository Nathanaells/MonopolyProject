import { useEffect, useRef, useState } from "react";
import type {DiceProps} from "../Interfaces/Interface"


const dotPosition: Record<number, Array<[number, number]>> = {
  1: [[50, 50]],
  2: [
    [25, 25],
    [75, 75],
  ],
  3: [
    [25, 25],
    [50, 50],
    [75, 75],
  ],
  4: [
    [25, 25],
    [75, 25],
    [25, 75],
    [75, 75],
  ],
  5: [
    [25, 25],
    [75, 25],
    [50, 50],
    [25, 75],
    [75, 75],
  ],
  6: [
    [25, 25],
    [75, 25],
    [25, 50],
    [75, 50],
    [25, 75],
    [75, 75],
  ],
};

function DiceFace({
  value,
  className = "",
}: {
  value: number;
  className?: string;
}) {
  const dots = dotPosition[value] ?? dotPosition[1];
  return (
    <div
      className={`relative w-14 h-14 rounded-xl shadow-lg select-none ${className}`}
      style={{
        background: "linear-gradient(145deg, #fefefe 0%, #e0e0e0 100%)",
        boxShadow:
          "0 4px 12px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.8)",
      }}
    >
      {dots.map(([x, y], i) => (
        <div
          key={i}
          className="absolute w-2.5 h-2.5 rounded-full bg-zinc-900"
          style={{
            left: `${x}%`,
            top: `${y}%`,
            transform: "translate(-50%, -50%)",
          }}
        />
      ))}
    </div>
  );
}

export default function DiceAnimation({ die1, die2, rolling, onDone }: DiceProps) {
  const [display1, setDisplay1] = useState(die1);
  const [display2, setDisplay2] = useState(die2);
  const [shake, setShake] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (rolling) {
      setShake(true);
      intervalRef.current = setInterval(() => {
        setDisplay1(Math.ceil(Math.random() * 6));
        setDisplay2(Math.ceil(Math.random() * 6));
      }, 80);

      timeoutRef.current = setTimeout(() => {
        if (intervalRef.current) clearInterval(intervalRef.current);
        setDisplay1(die1);
        setDisplay2(die2);
        setShake(false);
        onDone?.();
      }, 1400);
    } else {
      setDisplay1(die1);
      setDisplay2(die2);
      setShake(false);
    }

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, [rolling, die1, die2]);

  return (
    <div className="flex gap-4 items-center justify-center">
      <div className={shake ? "animate-dice-roll" : ""}>
        <DiceFace value={display1} />
      </div>
      <div className={shake ? "animate-dice-roll animation-delay-100" : ""}>
        <DiceFace value={display2} />
      </div>

      <style>{`
        @keyframes diceRoll {
          0%   { transform: rotate(0deg) scale(1); }
          15%  { transform: rotate(-18deg) scale(1.08); }
          30%  { transform: rotate(14deg) scale(0.95); }
          45%  { transform: rotate(-10deg) scale(1.05); }
          60%  { transform: rotate(8deg) scale(0.97); }
          75%  { transform: rotate(-5deg) scale(1.02); }
          90%  { transform: rotate(3deg) scale(0.99); }
          100% { transform: rotate(0deg) scale(1); }
        }
        .animate-dice-roll {
          animation: diceRoll 0.35s ease-in-out infinite;
        }
        .animation-delay-100 {
          animation-delay: 0.1s;
        }
      `}</style>
    </div>
  );
}
