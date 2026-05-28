import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, IconButton } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { RowData } from './types';
import { ServiceFormQuestionDto, ServiceFormQuestionType } from '../../../../components/Global/Services';
import { getCellError } from './validation';
import CellEditor from './CellEditor';
import './styles.css';

interface FullScreenTableModalProps {
  open: boolean;
  title: string;
  headers: string[];
  labels: string[];
  rows: RowData[];
  questions?: ServiceFormQuestionDto[];
  editable?: boolean;
  onCellEdit?: (rowIndex: number, questionCode: string, value: string) => void;
  onClose: () => void;
}

const FullScreenTableModal: React.FC<FullScreenTableModalProps> = ({
  open,
  title,
  headers,
  labels,
  rows,
  questions = [],
  editable = false,
  onCellEdit,
  onClose,
}) => {
  const [editingCell, setEditingCell] = useState<{
    rowIndex: number;
    header: string;
  } | null>(null);

  const getQuestionByCode = (code: string): ServiceFormQuestionDto | undefined => {
    return questions.find((q) => q.code === code);
  };

  const getDisplayValue = (value: string, question: ServiceFormQuestionDto | undefined): string => {
    if (!value || !question || !question.options || question.options.length === 0) {
      return value;
    }

    // For checkbox (multi-select), convert each value to label
    if (question.questionType === ServiceFormQuestionType.Checkbox) {
      const values = value.split(',').map((v) => v.trim());
      const labels = values.map((v) => {
        const option = question.options.find((opt) => opt.value === v);
        return option ? option.label : v;
      });
      return labels.join(', ');
    }

    // For single-select (Radio, Select), find the matching option
    const option = question.options.find((opt) => opt.value === value);
    return option ? option.label : value;
  };

  const handleCellClick = (rowIndex: number, header: string) => {
    if (!editable) return;
    setEditingCell({ rowIndex, header });
  };

  const handleCellSave = (value: string) => {
    if (editingCell && onCellEdit) {
      onCellEdit(editingCell.rowIndex, editingCell.header, value);
      setEditingCell(null);
    }
  };

  const handleCellCancel = () => {
    setEditingCell(null);
  };

  return (
    <Dialog open={open} onClose={onClose} fullScreen>
      <DialogTitle
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          borderBottom: '1px solid #b1b4b6',
          padding: '15px 20px',
        }}
      >
        <span style={{ fontSize: '24px', fontWeight: 700, color: '#0b0c0c' }}>{title}</span>
        <IconButton onClick={onClose} aria-label="Close">
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent sx={{ padding: 0 }}>
        <div style={{ overflow: 'auto', height: '100%' }}>
          <table className="bulk-upload-table" style={{ minWidth: '100%' }}>
            <thead>
              <tr>
                <th className="bulk-upload-table__row-number" style={{ position: 'sticky', left: 0 }}>
                  #
                </th>
                {headers.map((header, index) => (
                  <th key={header} style={{ minWidth: '180px' }}>
                    <span className="bulk-upload-table__header-code">{header}</span>
                    {labels[index] || 'Header'}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, rowIndex) => (
                <tr key={row.rowIndex}>
                  <td
                    className="bulk-upload-table__row-number"
                    style={{ position: 'sticky', left: 0 }}
                  >
                    {rowIndex + 1}
                  </td>
                  {headers.map((header) => {
                    const cellError = getCellError(row, header);
                    const isEditing =
                      editingCell?.rowIndex === row.rowIndex && editingCell?.header === header;
                    const cellValue = row.data[header] || '';
                    const question = getQuestionByCode(header);

                    if (isEditing) {
                      return (
                        <td
                          key={`${row.rowIndex}-${header}`}
                          className="bulk-upload-table__cell--editing"
                        >
                          <CellEditor
                            value={cellValue}
                            question={question}
                            error={cellError}
                            onSave={handleCellSave}
                            onCancel={handleCellCancel}
                          />
                        </td>
                      );
                    }

                    const displayValue = getDisplayValue(cellValue, question);

                    return (
                      <td
                        key={`${row.rowIndex}-${header}`}
                        className={`${editable ? 'bulk-upload-table__cell--editable' : ''}${cellError ? ' bulk-upload-table__cell--error' : ''}`}
                        onClick={() => handleCellClick(row.rowIndex, header)}
                        title={cellError?.message}
                      >
                        {displayValue || (cellError ? '(empty)' : '')}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </DialogContent>

      <DialogActions sx={{ padding: '15px 20px', borderTop: '1px solid #b1b4b6' }}>
        <button type="button" className="bulk-upload-button" onClick={onClose}>
          Close
        </button>
      </DialogActions>
    </Dialog>
  );
};

export default FullScreenTableModal;
