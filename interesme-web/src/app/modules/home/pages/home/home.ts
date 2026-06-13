import { Component } from '@angular/core';

type Initiative = {
  title: string;
  description: string;
  tags: string[];
  members: string;
  roles: number;
  tone: 'startup' | 'band' | 'garden';
};

type Message = {
  name: string;
  text: string;
  time: string;
  initials: string;
};

@Component({
  selector: 'app-home-page',
  standalone: true,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomePage {
  readonly initiatives: Initiative[] = [
    {
      title: 'Student Startup Weekend',
      description: 'Build a startup idea with students who want to create, test and present something real.',
      tags: ['Startup', 'Students', 'Teamwork'],
      members: '4/8',
      roles: 2,
      tone: 'startup',
    },
    {
      title: 'Create an Indie Band',
      description: 'Looking for musicians to write original songs, rehearse together and play live.',
      tags: ['Music', 'Creative', 'Band'],
      members: '2/5',
      roles: 3,
      tone: 'band',
    },
    {
      title: 'Community Garden Project',
      description: 'Create a small green space with people who care about nature and local community.',
      tags: ['Community', 'Nature', 'Local'],
      members: '6/12',
      roles: 4,
      tone: 'garden',
    },
  ];

  readonly messages: Message[] = [
    {
      name: 'Alex Johnson',
      text: 'I saw your initiative. Can I join?',
      time: '2m',
      initials: 'AJ',
    },
    {
      name: 'Sarah Chen',
      text: 'Let’s discuss the project idea.',
      time: '1h',
      initials: 'SC',
    },
    {
      name: 'Mike Ross',
      text: 'I can help with design.',
      time: '3h',
      initials: 'MR',
    },
  ];

  readonly trending = [
    'Student Startup Weekend',
    'Create an Indie Band',
    'Mobile Game Jam',
    'Community Garden',
  ];
}